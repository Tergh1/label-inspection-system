using System.Text.Json.Serialization;

namespace client.Contracts;

public sealed class MlWebhookResultHolder
{
    [JsonPropertyName("image_id")]
    public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("results")]
    public List<MlWebhookResult> Results { get; set; } = [];
}
