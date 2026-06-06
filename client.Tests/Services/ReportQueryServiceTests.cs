using System.Text;
using client.Data.Entities;
using client.Services;
using client.Tests.TestSupport;

namespace client.Tests.Services;

public sealed class ReportQueryServiceTests
{
    private readonly ReportQueryService service = new(null!);

    [Fact]
    public void BuildOptions_ReturnsDistinctTemplatesAndModelNamesSortedCaseInsensitively()
    {
        var templateB = InspectionEntityFactory.Template(friendlyName: "Beta");
        var templateA = InspectionEntityFactory.Template(friendlyName: "alpha");
        var rows = new[]
        {
            ImageWithModel(templateB, "ORB"),
            ImageWithModel(templateA, "ssim"),
            ImageWithModel(templateA, "SSIM")
        };

        var options = service.BuildOptions(rows);

        Assert.Equal(["alpha", "Beta"], options.Templates.Select(x => x.FriendlyName).ToArray());
        Assert.Equal(["ORB", "ssim"], options.ModelNames.ToArray());
    }

    [Fact]
    public void ApplyFilter_AppliesTemplateModelStatusOutcomeAndUploadedDateFilters()
    {
        var template = InspectionEntityFactory.Template();
        var otherTemplate = InspectionEntityFactory.Template();
        var matching = ImageWithModel(
            template,
            "SSIM",
            ProcessingStatus.Completed,
            OutcomeStatus.Valid,
            new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero));

        var rows = new[]
        {
            matching,
            ImageWithModel(otherTemplate, "SSIM", createdAtUtc: matching.CreatedAtUtc),
            ImageWithModel(template, "ORB", createdAtUtc: matching.CreatedAtUtc),
            ImageWithModel(template, "SSIM", ProcessingStatus.Failed, OutcomeStatus.Pending, matching.CreatedAtUtc),
            ImageWithModel(template, "SSIM", createdAtUtc: matching.CreatedAtUtc.AddDays(3))
        };

        var filtered = service.ApplyFilter(rows, new ReportFilter(
            template.Id,
            "ssim",
            ProcessingStatus.Completed,
            OutcomeStatus.Valid,
            new DateTimeOffset(2026, 1, 9, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 11, 0, 0, 0, TimeSpan.Zero)));

        var result = Assert.Single(filtered);
        Assert.Same(matching, result);
    }

    [Fact]
    public void ComputeStatistics_CountsStatusesOutcomesTemplatesAndModels()
    {
        var templateA = InspectionEntityFactory.Template();
        var templateB = InspectionEntityFactory.Template();
        var rows = new[]
        {
            ImageWithModel(templateA, "SSIM", ProcessingStatus.PendingDispatch, OutcomeStatus.Pending),
            ImageWithModel(templateA, "SSIM", ProcessingStatus.Queued, OutcomeStatus.Valid),
            ImageWithModel(templateB, "ORB", ProcessingStatus.Processing, OutcomeStatus.ValidWithDefects),
            ImageWithModel(templateB, "ORB", ProcessingStatus.Completed, OutcomeStatus.Invalid),
            ImageWithModel(templateB, "ORB", ProcessingStatus.Failed, OutcomeStatus.Pending)
        };

        var stats = service.ComputeStatistics(rows);

        Assert.Equal(5, stats.TotalImages);
        Assert.Equal(2, stats.TemplateCount);
        Assert.Equal(2, stats.ModelCount);
        Assert.Equal(1, stats.PendingDispatch);
        Assert.Equal(1, stats.Queued);
        Assert.Equal(1, stats.Processing);
        Assert.Equal(1, stats.Completed);
        Assert.Equal(1, stats.Failed);
        Assert.Equal(1, stats.InspectedImages);
        Assert.Equal(2, stats.OutcomePending);
        Assert.Equal(1, stats.Valid);
        Assert.Equal(1, stats.ValidWithDefects);
        Assert.Equal(1, stats.Invalid);
    }

    [Fact]
    public async Task WriteCsvAsync_EscapesFieldsAndIncludesModelSummaries()
    {
        var template = InspectionEntityFactory.Template(friendlyName: "Template, One");
        var image = ImageWithModel(template, "SSIM");
        image.OriginalFileName = "label \"bad\".png";
        image.MlResultsJson = """
            [
              {
                "model": "SSIM",
                "similarity_percent": 98.25,
                "defects": [{ "x": 1, "y": 2, "width": 3, "height": 4 }]
              }
            ]
            """;

        await using var stream = new MemoryStream();
        await service.WriteCsvAsync(stream, [image], CancellationToken.None);

        var csv = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Contains("\"Template, One\"", csv);
        Assert.Contains("\"label \"\"bad\"\".png\"", csv);
        Assert.Contains("SSIM: 98.25%", csv);
        Assert.Contains("SSIM: 1", csv);
    }

    [Fact]
    public void WriteXlsx_WritesReportHeadersAndRows()
    {
        var template = InspectionEntityFactory.Template(friendlyName: "Template One");
        var image = ImageWithModel(template, "SSIM");

        using var stream = new MemoryStream();
        service.WriteXlsx(stream, [image]);
        stream.Position = 0;

        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheet("Report");

        Assert.Equal("Template", sheet.Cell(1, 1).GetString());
        Assert.Equal("Image", sheet.Cell(1, 2).GetString());
        Assert.Equal("Template One", sheet.Cell(2, 1).GetString());
        Assert.Equal(image.OriginalFileName, sheet.Cell(2, 2).GetString());
    }

    private static InspectionImage ImageWithModel(
        InspectionTemplate template,
        string model,
        ProcessingStatus processingStatus = ProcessingStatus.Completed,
        OutcomeStatus outcomeStatus = OutcomeStatus.Valid,
        DateTimeOffset? createdAtUtc = null)
    {
        var image = InspectionEntityFactory.Image(
            templateId: template.Id,
            template: template,
            processingStatus: processingStatus,
            outcomeStatus: outcomeStatus,
            createdAtUtc: createdAtUtc);

        image.MlResultsJson = $$"""
            [{ "model": "{{model}}", "similarity_percent": 98, "defects": [] }]
            """;

        return image;
    }
}
