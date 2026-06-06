using client.Services;
using client.Tests.TestSupport;

namespace client.Tests.Services;

public sealed class InspectionMlResultParserTests
{
    [Fact]
    public void ParseModelResults_WhenMlResultsJsonContainsModels_ReturnsEachModelAndValidDefects()
    {
        var image = InspectionEntityFactory.Image();
        image.MlResultsJson = """
            [
              {
                "model": "SSIM",
                "similarity_percent": "98.25",
                "status": "completed",
                "defects": [
                  { "x": "1.5", "y": 2, "width": 3, "height": 4 },
                  { "x": 1, "y": 2, "width": 0, "height": 4 }
                ]
              },
              {
                "model": "ORB",
                "error": "could not inspect",
                "defects": []
              }
            ]
            """;

        var results = InspectionMlResultParser.ParseModelResults(image);

        Assert.Equal(2, results.Count);
        Assert.Equal("SSIM", results[0].Model);
        Assert.Equal(98.25m, results[0].SimilarityPercent);
        Assert.Equal("completed", results[0].Status);
        var defect = Assert.Single(results[0].Defects);
        Assert.Equal(1.5m, defect.X);
        Assert.Equal(2m, defect.Y);
        Assert.Equal(3m, defect.Width);
        Assert.Equal(4m, defect.Height);
        Assert.Equal("ORB", results[1].Model);
        Assert.Equal("could not inspect", results[1].FailureReason);
    }

    [Fact]
    public void ParseModelResults_WhenMlResultsJsonIsMalformed_FallsBackToLegacyFields()
    {
        var image = InspectionEntityFactory.Image();
        image.MlResultsJson = "{not json";
        image.SimilarityPercent = 88.5m;
        image.FailureReason = "dispatch failed";
        image.DefectsJson = """
            [
              { "x": "5", "y": "6", "width": "7", "height": "8" },
              { "x": 1, "y": 2, "width": -1, "height": 4 }
            ]
            """;

        var result = Assert.Single(InspectionMlResultParser.ParseModelResults(image));

        Assert.Equal("Result", result.Model);
        Assert.Equal(88.5m, result.SimilarityPercent);
        Assert.Equal("dispatch failed", result.FailureReason);
        var defect = Assert.Single(result.Defects);
        Assert.Equal(5m, defect.X);
        Assert.Equal(6m, defect.Y);
        Assert.Equal(7m, defect.Width);
        Assert.Equal(8m, defect.Height);
    }

    [Fact]
    public void ParseModelResults_WhenJsonHasNoUsableModelResults_ReturnsFallbackResult()
    {
        var image = InspectionEntityFactory.Image();
        image.MlResultsJson = """[null, "ignored"]""";
        image.SimilarityPercent = 91.2m;

        var result = Assert.Single(InspectionMlResultParser.ParseModelResults(image));

        Assert.Equal("Result", result.Model);
        Assert.Equal(91.2m, result.SimilarityPercent);
    }
}
