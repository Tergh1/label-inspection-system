using client.Data;
using client.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using client.Options;

namespace client.Endpoints;

public static class InspectionFileEndpoints
{
    public static IEndpointRouteBuilder MapInspectionFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var storageOptions = endpoints.ServiceProvider.GetRequiredService<IOptions<InspectionStorageOptions>>().Value;
        var publicFileRoute = $"{NormalizePath(storageOptions.PublicFilePathPrefix)}/{{token}}";

        endpoints.MapGet(publicFileRoute, async (
            string token,
            ApplicationDbContext dbContext,
            InspectionFileStorage fileStorage,
            CancellationToken cancellationToken) =>
        {
            var inspectionImage = await dbContext.InspectionImages
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.PublicAccessToken == token, cancellationToken);

            if (inspectionImage is null)
            {
                return Results.NotFound();
            }

            var filePath = fileStorage.GetAbsolutePath(inspectionImage.StoredRelativePath);
            if (!File.Exists(filePath))
            {
                return Results.NotFound();
            }

            return Results.File(filePath, inspectionImage.ContentType, enableRangeProcessing: false);
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
