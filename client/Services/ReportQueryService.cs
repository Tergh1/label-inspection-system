using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using client.Data.Entities;

namespace client.Services;

public sealed record ReportFilter(
    Guid? TemplateId,
    string? ModelName,
    ProcessingStatus? ProcessingStatus,
    OutcomeStatus? OutcomeStatus,
    DateTimeOffset? UploadedFromUtc,
    DateTimeOffset? UploadedToUtc);

public sealed record TemplateOption(Guid Id, string FriendlyName);

public sealed record ReportFilterOptions(
    IReadOnlyList<TemplateOption> Templates,
    IReadOnlyList<string> ModelNames);

public sealed record ReportStatistics(
    int TotalImages,
    int TemplateCount,
    int ModelCount,
    int PendingDispatch,
    int Queued,
    int Processing,
    int Completed,
    int Failed,
    int InspectedImages,
    int OutcomePending,
    int Valid,
    int ValidWithDefects,
    int Invalid);

public sealed class ReportQueryService(InspectionWorkflowService workflowService)
{
    public Task<IReadOnlyList<InspectionImage>> LoadAsync(string ownerUserId, CancellationToken cancellationToken)
        => workflowService.GetUserImagesAsync(ownerUserId, cancellationToken);

    public ReportFilterOptions BuildOptions(IReadOnlyList<InspectionImage> rows)
    {
        var templates = rows
            .Where(x => x.Template is not null)
            .GroupBy(x => x.Template!.Id)
            .Select(g => new TemplateOption(g.Key, g.First().Template!.FriendlyName))
            .OrderBy(x => x.FriendlyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var modelNames = rows
            .SelectMany(InspectionMlResultParser.ParseModelResults)
            .Select(x => x.Model)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ReportFilterOptions(templates, modelNames);
    }

    public IReadOnlyList<InspectionImage> ApplyFilter(IEnumerable<InspectionImage> rows, ReportFilter filter)
    {
        IEnumerable<InspectionImage> query = rows;

        if (filter.TemplateId.HasValue)
        {
            query = query.Where(x => x.TemplateId == filter.TemplateId.Value);
        }

        if (filter.ProcessingStatus.HasValue)
        {
            query = query.Where(x => x.ProcessingStatus == filter.ProcessingStatus.Value);
        }

        if (filter.OutcomeStatus.HasValue)
        {
            query = query.Where(x => x.OutcomeStatus == filter.OutcomeStatus.Value);
        }

        if (filter.UploadedFromUtc.HasValue)
        {
            var from = filter.UploadedFromUtc.Value;
            query = query.Where(x => x.CreatedAtUtc >= from);
        }

        if (filter.UploadedToUtc.HasValue)
        {
            var to = filter.UploadedToUtc.Value;
            query = query.Where(x => x.CreatedAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.ModelName))
        {
            var modelName = filter.ModelName;
            query = query.Where(x => InspectionMlResultParser
                .ParseModelResults(x)
                .Any(m => string.Equals(m.Model, modelName, StringComparison.OrdinalIgnoreCase)));
        }

        return query.ToList();
    }

    public ReportStatistics ComputeStatistics(IReadOnlyCollection<InspectionImage> filtered)
    {
        var templateCount = filtered
            .Where(x => x.TemplateId.HasValue)
            .Select(x => x.TemplateId!.Value)
            .Distinct()
            .Count();

        var modelCount = filtered
            .SelectMany(InspectionMlResultParser.ParseModelResults)
            .Select(x => x.Model)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return new ReportStatistics(
            TotalImages: filtered.Count,
            TemplateCount: templateCount,
            ModelCount: modelCount,
            PendingDispatch: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.PendingDispatch),
            Queued: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.Queued),
            Processing: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.Processing),
            Completed: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.Completed),
            Failed: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.Failed),
            InspectedImages: filtered.Count(x => x.ProcessingStatus == ProcessingStatus.Completed),
            OutcomePending: filtered.Count(x => x.OutcomeStatus == OutcomeStatus.Pending),
            Valid: filtered.Count(x => x.OutcomeStatus == OutcomeStatus.Valid),
            ValidWithDefects: filtered.Count(x => x.OutcomeStatus == OutcomeStatus.ValidWithDefects),
            Invalid: filtered.Count(x => x.OutcomeStatus == OutcomeStatus.Invalid));
    }

    public async Task WriteCsvAsync(Stream output, IEnumerable<InspectionImage> rows, CancellationToken cancellationToken)
    {
        await using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), leaveOpen: true);

        var headers = new[]
        {
            "Template",
            "Image",
            "Processing Status",
            "Uploaded (UTC)",
            "Acceptance Similarity (%)",
            "Calculated Similarities (per model)",
            "Inspection Status",
            "Defects (per model)"
        };

        await writer.WriteLineAsync(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var image in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var modelResults = InspectionMlResultParser.ParseModelResults(image);

            var similarities = string.Join(" | ", modelResults
                .Select(x => $"{x.Model}: {(x.SimilarityPercent.HasValue ? x.SimilarityPercent.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%" : "N/A")}"));

            var defects = string.Join(" | ", modelResults
                .Select(x => $"{x.Model}: {x.Defects.Count}"));

            var fields = new[]
            {
                image.Template?.FriendlyName ?? "-",
                image.OriginalFileName,
                image.ProcessingStatus.ToString(),
                image.CreatedAtUtc.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                image.MinimumSimilarityPercent.ToString("0.##", CultureInfo.InvariantCulture),
                similarities,
                image.OutcomeStatus.ToString(),
                defects
            };

            await writer.WriteLineAsync(string.Join(",", fields.Select(EscapeCsv)));
        }

        await writer.FlushAsync();
    }

    public void WriteXlsx(Stream output, IEnumerable<InspectionImage> rows)
    {
        var materialized = rows as IReadOnlyCollection<InspectionImage> ?? rows.ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");

        string[] headers =
        [
            "Template",
            "Image",
            "Processing Status",
            "Uploaded (UTC)",
            "Acceptance Similarity (%)",
            "Calculated Similarities (per model)",
            "Inspection Status",
            "Defects (per model)"
        ];

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var image in materialized)
        {
            var modelResults = InspectionMlResultParser.ParseModelResults(image);

            var similarities = string.Join(" | ", modelResults
                .Select(x => $"{x.Model}: {(x.SimilarityPercent.HasValue ? x.SimilarityPercent.Value.ToString("0.##", CultureInfo.InvariantCulture) + "%" : "N/A")}"));

            var defects = string.Join(" | ", modelResults
                .Select(x => $"{x.Model}: {x.Defects.Count}"));

            sheet.Cell(rowIndex, 1).Value = image.Template?.FriendlyName ?? "-";
            sheet.Cell(rowIndex, 2).Value = image.OriginalFileName;
            sheet.Cell(rowIndex, 3).Value = image.ProcessingStatus.ToString();
            sheet.Cell(rowIndex, 4).Value = image.CreatedAtUtc.UtcDateTime;
            sheet.Cell(rowIndex, 4).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss";
            sheet.Cell(rowIndex, 5).Value = (double)image.MinimumSimilarityPercent;
            sheet.Cell(rowIndex, 6).Value = similarities;
            sheet.Cell(rowIndex, 7).Value = image.OutcomeStatus.ToString();
            sheet.Cell(rowIndex, 8).Value = defects;

            rowIndex++;
        }

        sheet.Columns().AdjustToContents();
        workbook.SaveAs(output);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuoting = value.IndexOfAny(['"', ',', '\n', '\r']) >= 0;
        var escaped = value.Replace("\"", "\"\"");
        return needsQuoting ? $"\"{escaped}\"" : escaped;
    }
}
