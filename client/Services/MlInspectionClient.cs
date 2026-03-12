using client.Options;
using Microsoft.Extensions.Options;

namespace client.Services;

public sealed class MlInspectionClient(HttpClient httpClient, IOptions<InspectionMlOptions> options)
{
    public async Task DispatchAsync(Guid imageId, string imageUrl, string templateUrl, string callbackUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.InspectPath);
        using var content = JsonContent.Create(new
        {
            image_url = imageUrl,
            template_url = templateUrl,
            callback_url = callbackUrl,
            image_id = imageId
        });

        request.Content = content;

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(options.Value.ApiKeyHeaderName, options.Value.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
