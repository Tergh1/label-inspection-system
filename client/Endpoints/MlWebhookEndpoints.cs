using client.Contracts;
using client.Data;
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

            var normalizedResults = MlWebhookResultProcessor.NormalizeResults(payload.Results);

            if (normalizedResults.Count == 0)
            {
                return Results.BadRequest(new { error = "At least one result item is required." });
            }

            MlWebhookResultProcessor.ApplyTo(inspectionImage, normalizedResults, DateTimeOffset.UtcNow);

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
