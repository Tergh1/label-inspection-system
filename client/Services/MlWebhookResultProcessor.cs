using System.Text.Json;
using System.Text.Json.Nodes;
using client.Contracts;
using client.Data.Entities;

namespace client.Services;

public static class MlWebhookResultProcessor
{
    public static List<MlWebhookResult> NormalizeResults(IEnumerable<MlWebhookResult?> results)
    {
        return results
            .Where(static x => x is not null)
            .Select(static x =>
            {
                x!.Model = (x.Model ?? string.Empty).Trim();
                x.Error = string.IsNullOrWhiteSpace(x.Error) ? null : x.Error.Trim();
                x.FailureReason = string.IsNullOrWhiteSpace(x.FailureReason) ? null : x.FailureReason.Trim();
                x.Status = string.IsNullOrWhiteSpace(x.Status) ? null : x.Status.Trim();
                return x;
            })
            .ToList();
    }

    public static void ApplyTo(InspectionImage inspectionImage, IReadOnlyList<MlWebhookResult> normalizedResults, DateTimeOffset updatedAtUtc)
    {
        inspectionImage.UpdatedAtUtc = updatedAtUtc;
        inspectionImage.MlResultsJson = JsonSerializer.Serialize(normalizedResults, JsonSerializerOptions.Default);
        inspectionImage.SimilarityPercent = GetMinimumSimilarityPercent(normalizedResults);
        inspectionImage.DefectsJson = BuildCombinedDefectsJson(normalizedResults);
        inspectionImage.FailureReason = BuildFailureSummary(normalizedResults);
        inspectionImage.ProcessingStatus = GetProcessingStatus(normalizedResults);
        inspectionImage.OutcomeStatus = GetOutcomeStatus(normalizedResults, inspectionImage.MinimumSimilarityPercent, inspectionImage.ProcessingStatus);
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

        if (successfulResults.Any(x => x.SimilarityPercent.HasValue && x.SimilarityPercent.Value < minimumSimilarityPercent))
        {
            return OutcomeStatus.Invalid;
        }

        if (successfulResults.Any(x => x.Defects is not null && x.Defects.Count > 0))
        {
            return OutcomeStatus.ValidWithDefects;
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
