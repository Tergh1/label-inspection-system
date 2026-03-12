using client.Data.Entities;
using client.Options;
using Microsoft.Extensions.Options;

namespace client.Services;

public sealed class InspectionUrlBuilder(IOptions<InspectionMlOptions> mlOptions, IOptions<InspectionStorageOptions> storageOptions)
{
    public string BuildImageUrl(InspectionImage image)
    {
        var baseUri = TrimTrailingSlash(mlOptions.Value.PublicAppBaseUrl);
        var pathPrefix = NormalizePath(storageOptions.Value.PublicFilePathPrefix);
        return $"{baseUri}{pathPrefix}/{image.PublicAccessToken}";
    }

    public string BuildTemplateUrl(InspectionTemplate template)
    {
        var baseUri = TrimTrailingSlash(mlOptions.Value.PublicAppBaseUrl);
        var pathPrefix = NormalizePath(storageOptions.Value.TemplatePublicFilePathPrefix);
        return $"{baseUri}{pathPrefix}/{template.PublicAccessToken}";
    }

    public string BuildWebhookUrl()
    {
        var baseUri = TrimTrailingSlash(mlOptions.Value.PublicAppBaseUrl);
        var webhookPath = NormalizePath(mlOptions.Value.WebhookPath);
        return $"{baseUri}{webhookPath}";
    }

    private static string NormalizePath(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.StartsWith('/') ? value : $"/{value}";
    }

    private static string TrimTrailingSlash(string value) => value.TrimEnd('/');
}
