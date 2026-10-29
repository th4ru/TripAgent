namespace TripPlanner.Infrastructure.N8n;

public class N8nOptions
{
    public const string SectionName = "N8n";

    /// <summary>Production webhook URL of the n8n workflow (or Chat Trigger).</summary>
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>Optional header name used to send the API key. Defaults to X-N8N-Api-Key.</summary>
    public string ApiKeyHeader { get; set; } = "X-N8N-Api-Key";

    /// <summary>Optional API key / bearer token value for the webhook.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Request timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}
