namespace client.Options;

public sealed class InspectionMlOptions
{
    public const string SectionName = "InspectionMl";

    public string BaseUrl { get; set; } = "http://localhost:8000";

    public string InspectPath { get; set; } = "/inspect-async";

    public string ApiKey { get; set; } = string.Empty;

    public string ApiKeyHeaderName { get; set; } = "X-API-KEY";

    public string PublicAppBaseUrl { get; set; } = "https://localhost:5001";

    public string WebhookPath { get; set; } = "/api/ml/webhook";

    public string WebhookSecret { get; set; } = "change-me";

    public string WebhookSecretHeaderName { get; set; } = "X-Webhook-Secret";
}
