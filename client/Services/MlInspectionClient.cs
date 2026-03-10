using client.Options;
using Microsoft.Extensions.Options;

namespace client.Services;

public sealed class MlInspectionClient(HttpClient httpClient, IOptions<InspectionMlOptions> options)
{
    public async Task DispatchAsync(Guid imageId, string imageUrl, string callbackUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.Value.InspectPath);
        using var content = new MultipartFormDataContent
        {
            { new StringContent(imageUrl), "image_url" },
            { new StringContent(callbackUrl), "callback_url" },
            { new StringContent(imageId.ToString()), "image_id" }
        };

        request.Content = content;

        if (!string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(options.Value.ApiKeyHeaderName, options.Value.ApiKey);
        }

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
