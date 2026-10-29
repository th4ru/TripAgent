using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripPlanner.Application.Chat;

namespace TripPlanner.Infrastructure.N8n;

public class N8nChatAgent : IChatAgent
{
    private static readonly string[] ReplyKeys =
    {
        "reply", "output", "text", "answer", "response", "message", "content"
    };

    private readonly HttpClient _httpClient;
    private readonly N8nOptions _options;
    private readonly ILogger<N8nChatAgent> _logger;

    public N8nChatAgent(HttpClient httpClient, IOptions<N8nOptions> options, ILogger<N8nChatAgent> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            string header = string.IsNullOrWhiteSpace(_options.ApiKeyHeader)
                ? "X-N8N-Api-Key"
                : _options.ApiKeyHeader;

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header, _options.ApiKey);
        }
    }

    public async Task<ChatReply> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        string sessionId = string.IsNullOrWhiteSpace(request.SessionId)
            ? Guid.NewGuid().ToString("N")
            : request.SessionId!;

        // Payload works with the n8n Chat Trigger (chatInput) and with plain Webhook nodes (message).
        var payload = new
        {
            action = "sendMessage",
            sessionId,
            chatInput = request.Message,
            message = request.Message
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            _options.WebhookUrl,
            payload,
            cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("n8n webhook returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"n8n webhook responded with {(int)response.StatusCode}.");
        }

        return new ChatReply(ExtractReply(body), sessionId);
    }

    private static string ExtractReply(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                root = root[0];
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                string? direct = TryReadKeys(root);
                if (direct is not null)
                {
                    return direct;
                }

                if (root.TryGetProperty("json", out JsonElement json) && json.ValueKind == JsonValueKind.Object)
                {
                    string? nested = TryReadKeys(json);
                    if (nested is not null)
                    {
                        return nested;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not JSON - return the raw body.
        }

        return body.Trim();
    }

    private static string? TryReadKeys(JsonElement element)
    {
        foreach (string key in ReplyKeys)
        {
            if (!element.TryGetProperty(key, out JsonElement value))
            {
                continue;
            }

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.GetRawText();
        }

        return null;
    }
}
