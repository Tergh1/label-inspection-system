using System.Globalization;
using client.Data;
using client.Data.Entities;
using client.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace client.Endpoints;

public static class ReportExportEndpoints
{
    public static IEndpointRouteBuilder MapReportExportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/reports/export").RequireAuthorization();

        group.MapGet("/csv", async (
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ReportQueryService reportQueryService,
            [FromQuery] Guid? templateId,
            [FromQuery] string? modelName,
            [FromQuery] ProcessingStatus? processingStatus,
            [FromQuery] OutcomeStatus? outcomeStatus,
            [FromQuery] DateTime? uploadedFrom,
            [FromQuery] DateTime? uploadedTo,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var filter = BuildFilter(templateId, modelName, processingStatus, outcomeStatus, uploadedFrom, uploadedTo);
            var rows = await reportQueryService.LoadAsync(user.Id, cancellationToken);
            var filtered = reportQueryService.ApplyFilter(rows, filter);

            httpContext.Response.ContentType = "text/csv; charset=utf-8";
            httpContext.Response.Headers.ContentDisposition = $"attachment; filename={BuildFileName("csv")}";

            await reportQueryService.WriteCsvAsync(httpContext.Response.Body, filtered, cancellationToken);
            return Results.Empty;
        });

        group.MapGet("/xlsx", async (
            HttpContext httpContext,
            UserManager<ApplicationUser> userManager,
            ReportQueryService reportQueryService,
            [FromQuery] Guid? templateId,
            [FromQuery] string? modelName,
            [FromQuery] ProcessingStatus? processingStatus,
            [FromQuery] OutcomeStatus? outcomeStatus,
            [FromQuery] DateTime? uploadedFrom,
            [FromQuery] DateTime? uploadedTo,
            CancellationToken cancellationToken) =>
        {
            var user = await userManager.GetUserAsync(httpContext.User);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var filter = BuildFilter(templateId, modelName, processingStatus, outcomeStatus, uploadedFrom, uploadedTo);
            var rows = await reportQueryService.LoadAsync(user.Id, cancellationToken);
            var filtered = reportQueryService.ApplyFilter(rows, filter);

            using var memoryStream = new MemoryStream();
            reportQueryService.WriteXlsx(memoryStream, filtered);
            memoryStream.Position = 0;

            return Results.File(
                memoryStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                BuildFileName("xlsx"));
        });

        return endpoints;
    }

    private static ReportFilter BuildFilter(
        Guid? templateId,
        string? modelName,
        ProcessingStatus? processingStatus,
        OutcomeStatus? outcomeStatus,
        DateTime? uploadedFrom,
        DateTime? uploadedTo)
    {
        return new ReportFilter(
            templateId,
            string.IsNullOrWhiteSpace(modelName) ? null : modelName,
            processingStatus,
            outcomeStatus,
            uploadedFrom.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(uploadedFrom.Value.Date, DateTimeKind.Utc)) : null,
            uploadedTo.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(uploadedTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)) : null);
    }

    private static string BuildFileName(string extension)
        => $"report-{DateTime.UtcNow.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}.{extension}";
}
