using System.Text.Json;
using System.Text.Json.Nodes;
using client.Contracts;
using client.Data.Entities;
using client.Services;
using client.Tests.TestSupport;

namespace client.Tests.Services;

public sealed class MlWebhookResultProcessorTests
{
    [Fact]
    public void NormalizeResults_TrimsTextAndRemovesBlankOptionalFields()
    {
        var results = MlWebhookResultProcessor.NormalizeResults(
        [
            null,
            new MlWebhookResult
            {
                Model = " SSIM ",
                Status = " processing ",
                Error = " ",
                FailureReason = " failed "
            }
        ]);

        var result = Assert.Single(results);
        Assert.Equal("SSIM", result.Model);
        Assert.Equal("processing", result.Status);
        Assert.Null(result.Error);
        Assert.Equal("failed", result.FailureReason);
    }

    [Fact]
    public void ApplyTo_WhenSuccessfulResultHasDefects_CompletesAsValidWithDefects()
    {
        var image = InspectionEntityFactory.Image();
        image.MinimumSimilarityPercent = 95;
        var updatedAt = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var results = new[]
        {
            new MlWebhookResult
            {
                Model = "SSIM",
                SimilarityPercent = 98.1m,
                Defects =
                [
                    new JsonObject
                    {
                        ["x"] = 1,
                        ["y"] = 2,
                        ["width"] = 3,
                        ["height"] = 4
                    }
                ]
            },
            new MlWebhookResult
            {
                Model = "ORB",
                SimilarityPercent = 96.2m
            }
        };

        MlWebhookResultProcessor.ApplyTo(image, results, updatedAt);

        Assert.Equal(updatedAt, image.UpdatedAtUtc);
        Assert.Equal(ProcessingStatus.Completed, image.ProcessingStatus);
        Assert.Equal(OutcomeStatus.ValidWithDefects, image.OutcomeStatus);
        Assert.Equal(96.2m, image.SimilarityPercent);
        Assert.Null(image.FailureReason);
        Assert.NotNull(image.MlResultsJson);
        Assert.NotNull(image.DefectsJson);
        using var defectsDocument = JsonDocument.Parse(image.DefectsJson!);
        var defect = Assert.Single(defectsDocument.RootElement.EnumerateArray());
        Assert.Equal("SSIM", defect.GetProperty("model").GetString());
        Assert.Equal(3, defect.GetProperty("width").GetInt32());
    }

    [Fact]
    public void ApplyTo_WhenSimilarityIsBelowMinimum_MarksImageInvalid()
    {
        var image = InspectionEntityFactory.Image();
        image.MinimumSimilarityPercent = 95;

        MlWebhookResultProcessor.ApplyTo(image,
        [
            new MlWebhookResult
            {
                Model = "SSIM",
                SimilarityPercent = 94.99m
            }
        ], DateTimeOffset.UtcNow);

        Assert.Equal(ProcessingStatus.Completed, image.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Invalid, image.OutcomeStatus);
    }

    [Fact]
    public void ApplyTo_WhenAnyResultIsProcessing_KeepsOutcomePending()
    {
        var image = InspectionEntityFactory.Image();

        MlWebhookResultProcessor.ApplyTo(image,
        [
            new MlWebhookResult
            {
                Model = "SSIM",
                Status = "processing"
            },
            new MlWebhookResult
            {
                Model = "ORB",
                SimilarityPercent = 99
            }
        ], DateTimeOffset.UtcNow);

        Assert.Equal(ProcessingStatus.Processing, image.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Pending, image.OutcomeStatus);
    }

    [Fact]
    public void ApplyTo_WhenAllResultsFail_MarksFailedAndBuildsFailureSummary()
    {
        var image = InspectionEntityFactory.Image();

        MlWebhookResultProcessor.ApplyTo(image,
        [
            new MlWebhookResult
            {
                Model = "SSIM",
                FailureReason = "bad image"
            },
            new MlWebhookResult
            {
                Model = "",
                Error = "timeout"
            }
        ], DateTimeOffset.UtcNow);

        Assert.Equal(ProcessingStatus.Failed, image.ProcessingStatus);
        Assert.Equal(OutcomeStatus.Pending, image.OutcomeStatus);
        Assert.Equal("SSIM: bad image; Unknown model: timeout", image.FailureReason);
        Assert.Null(image.SimilarityPercent);
        Assert.Null(image.DefectsJson);
    }
}
