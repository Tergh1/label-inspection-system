using System.Text.Json;
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
            MlWebhookResult payload,
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

            var failureReason = payload.FailureReason ?? payload.Error;
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                inspectionImage.ProcessingStatus = ProcessingStatus.Failed;
                inspectionImage.OutcomeStatus = OutcomeStatus.Pending;
                inspectionImage.FailureReason = failureReason;
                await dbContext.SaveChangesAsync(cancellationToken);
                await inspectionUpdateNotifier.PublishAsync(inspectionImage.OwnerUserId, inspectionImage.Id);
                return Results.Ok(new { status = "updated" });
            }

            if (string.Equals(payload.Status, "processing", StringComparison.OrdinalIgnoreCase))
            {
                inspectionImage.ProcessingStatus = ProcessingStatus.Processing;
                await dbContext.SaveChangesAsync(cancellationToken);
                await inspectionUpdateNotifier.PublishAsync(inspectionImage.OwnerUserId, inspectionImage.Id);
                return Results.Ok(new { status = "updated" });
            }

            inspectionImage.ProcessingStatus = ProcessingStatus.Completed;
            inspectionImage.FailureReason = null;
            inspectionImage.SimilarityPercent = payload.SimilarityPercent;
            inspectionImage.DefectsJson = payload.Defects?.ToJsonString(JsonSerializerOptions.Default);

            if (payload.SimilarityPercent.HasValue)
            {
                var isValid = payload.SimilarityPercent.Value >= inspectionImage.MinimumSimilarityPercent;
                inspectionImage.OutcomeStatus = isValid
                    ? OutcomeStatus.Valid
                    : OutcomeStatus.Invalid;

                inspectionImage.OutcomeStatus = isValid && payload.Defects is not null && payload.Defects.Count > 0
                    ? OutcomeStatus.ValidWithDefects
                    : inspectionImage.OutcomeStatus;
            }

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
}
