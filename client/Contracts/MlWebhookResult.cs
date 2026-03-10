using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace client.Contracts;

public sealed class MlWebhookResult
{
    [JsonPropertyName("image_id")]
    public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("similarity_percent")]
    public decimal? SimilarityPercent { get; set; }

    [JsonPropertyName("defects")]
    public JsonArray? Defects { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("failure_reason")]
    public string? FailureReason { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
