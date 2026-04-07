using System.Text.Json.Serialization;

namespace client.Contracts;

public sealed class MlWebhookResultHolder
{
    [JsonPropertyName("image_id")]
    public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public List<MlWebhookResult> Result { get; set; } = [];
}
