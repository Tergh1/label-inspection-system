using System.Text.Json;
using System.Text.Json.Nodes;
using client.Contracts;
using client.Data;
using client.Data.Entities;
using client.Options;
using client.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace client.Endpoints;

public static class MlWebhookEndpoints
{
    public static IEndpointRouteBuilder MapMlWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<InspectionMlOptions>>().Value;
        var webhookRoute = NormalizePath(options.WebhookPath);

        endpoints.MapPost(webhookRoute, async (
            HttpContext httpContext,
            MlWebhookResultHolder payload,
            ApplicationDbContext dbContext,
            InspectionUpdateNotifier inspectionUpdateNotifier,
            IOptions<InspectionMlOptions> runtimeOptions,
            CancellationToken cancellationToken) =>
        {
            var secretHeaderName = runtimeOptions.Value.WebhookSecretHeaderName;
            if (!httpContext.Request.Headers.TryGetValue(secretHeaderName, out var providedSecret) ||
                !string.Equals(providedSecret.ToString(), runtimeOptions.Value.WebhookSecret, StringComparison.Ordinal))
            {
                return Results.Unauthorized();
            }

            if (!Guid.TryParse(payload.ImageId, out var imageId))
            {
                return Results.BadRequest(new { error = "Invalid image_id." });
            }

            var inspectionImage = await dbContext.InspectionImages.SingleOrDefaultAsync(x => x.Id == imageId, cancellationToken);
            if (inspectionImage is null)
            {
                return Results.NotFound();
            }

            inspectionImage.UpdatedAtUtc = DateTimeOffset.UtcNow;

            var normalizedResults = payload.Results
                .Where(static x => x is not null)
                .Select(static x =>
                {
                    x.Model = x.Model.Trim();
                    x.Error = string.IsNullOrWhiteSpace(x.Error) ? null : x.Error.Trim();
                    x.FailureReason = string.IsNullOrWhiteSpace(x.FailureReason) ? null : x.FailureReason.Trim();
                    x.Status = string.IsNullOrWhiteSpace(x.Status) ? null : x.Status.Trim();
                    return x;
                })
                .ToList();

            if (normalizedResults.Count == 0)
            {
                return Results.BadRequest(new { error = "At least one result item is required." });
            }

            inspectionImage.MlResultsJson = JsonSerializer.Serialize(normalizedResults, JsonSerializerOptions.Default);
            inspectionImage.SimilarityPercent = GetMinimumSimilarityPercent(normalizedResults);
            inspectionImage.DefectsJson = BuildCombinedDefectsJson(normalizedResults);
            inspectionImage.FailureReason = BuildFailureSummary(normalizedResults);
            inspectionImage.ProcessingStatus = GetProcessingStatus(normalizedResults);
            inspectionImage.OutcomeStatus = GetOutcomeStatus(normalizedResults, inspectionImage.MinimumSimilarityPercent, inspectionImage.ProcessingStatus);

            await dbContext.SaveChangesAsync(cancellationToken);
            await inspectionUpdateNotifier.PublishAsync(inspectionImage.OwnerUserId, inspectionImage.Id);
            return Results.Ok(new { status = "updated" });
        });

        return endpoints;
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.StartsWith('/') ? value : $"/{value}";
    }

    private static decimal? GetMinimumSimilarityPercent(IEnumerable<MlWebhookResult> results)
    {
        var similarities = results
            .Where(IsSuccessfulResult)
            .Where(x => x.SimilarityPercent.HasValue)
            .Select(x => x.SimilarityPercent!.Value)
            .ToList();

        return similarities.Count == 0 ? null : similarities.Min();
    }

    private static string? BuildCombinedDefectsJson(IEnumerable<MlWebhookResult> results)
    {
        var defects = new JsonArray();

        foreach (var result in results.Where(IsSuccessfulResult))
        {
            if (result.Defects is null)
            {
                continue;
            }

            foreach (var defectNode in result.Defects)
            {
                if (defectNode is not JsonObject defectObject)
                {
                    continue;
                }

                var combinedDefect = new JsonObject
                {
                    ["model"] = result.Model
                };

                foreach (var property in defectObject)
                {
                    combinedDefect[property.Key] = property.Value?.DeepClone();
                }

                defects.Add(combinedDefect);
            }
        }

        return defects.Count == 0 ? null : defects.ToJsonString(JsonSerializerOptions.Default);
    }

    private static string? BuildFailureSummary(IEnumerable<MlWebhookResult> results)
    {
        var failures = results
            .Select(x => new
            {
                Model = string.IsNullOrWhiteSpace(x.Model) ? "Unknown model" : x.Model,
                Reason = x.FailureReason ?? x.Error
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Reason))
            .Select(x => $"{x.Model}: {x.Reason}")
            .ToList();

        return failures.Count == 0 ? null : string.Join("; ", failures);
    }

    private static ProcessingStatus GetProcessingStatus(IEnumerable<MlWebhookResult> results)
    {
        if (results.Any(IsProcessingResult))
        {
            return ProcessingStatus.Processing;
        }

        return results.Any(IsSuccessfulResult)
            ? ProcessingStatus.Completed
            : ProcessingStatus.Failed;
    }

    private static OutcomeStatus GetOutcomeStatus(IEnumerable<MlWebhookResult> results, decimal minimumSimilarityPercent, ProcessingStatus processingStatus)
    {
        if (processingStatus == ProcessingStatus.Processing)
        {
            return OutcomeStatus.Pending;
        }

        var successfulResults = results.Where(IsSuccessfulResult).ToList();
        if (successfulResults.Count == 0)
        {
            return OutcomeStatus.Pending;
        }

        if (successfulResults.Any(x => x.Defects is not null && x.Defects.Count > 0))
        {
            return OutcomeStatus.ValidWithDefects;
        }

        if (successfulResults.Any(x => x.SimilarityPercent.HasValue && x.SimilarityPercent.Value < minimumSimilarityPercent))
        {
            return OutcomeStatus.Invalid;
        }

        return OutcomeStatus.Valid;
    }

    private static bool IsProcessingResult(MlWebhookResult result)
        => string.Equals(result.Status, "processing", StringComparison.OrdinalIgnoreCase);

    private static bool IsSuccessfulResult(MlWebhookResult result)
        => !IsProcessingResult(result)
           && string.IsNullOrWhiteSpace(result.FailureReason)
           && string.IsNullOrWhiteSpace(result.Error);
}
