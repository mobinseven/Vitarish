using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vitarish.Social.Infrastructure.Adapters;

[RegisterTransient]
internal sealed class InstagramSocialProviderAdapter(IHttpClientFactory httpClientFactory) : ISocialProviderAdapter
{
    private static readonly IReadOnlySet<SocialCapability> AdapterCapabilities = new HashSet<SocialCapability>
    {
        SocialCapability.WebhookInbound,
        SocialCapability.FetchProfile,
        SocialCapability.FetchComments,
        SocialCapability.FetchMessages,
    };

    public SocialProvider Provider => SocialProvider.Instagram;

    public IReadOnlySet<SocialCapability> Capabilities => AdapterCapabilities;

    public bool VerifyWebhook(IReadOnlyDictionary<string, string> headers, string body, string secret)
    {
        if (!headers.TryGetValue("X-Hub-Signature-256", out string? signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        byte[] key = Encoding.UTF8.GetBytes(secret);
        byte[] payload = Encoding.UTF8.GetBytes(body);
        using HMACSHA256 hmac = new(key);
        string expected = $"sha256={Convert.ToHexStringLower(hmac.ComputeHash(payload))}";

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader));
    }

    public SocialEvent? ParseWebhook(SocialConnection connection, RawWebhookEvent rawEvent)
    {
        using JsonDocument doc = JsonDocument.Parse(rawEvent.RawBody);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("entry", out JsonElement entry)
            && entry.ValueKind == JsonValueKind.Array
            && entry.GetArrayLength() > 0)
        {
            JsonElement firstEntry = entry[0];

            if (firstEntry.TryGetProperty("messaging", out JsonElement messaging)
                && messaging.ValueKind == JsonValueKind.Array
                && messaging.GetArrayLength() > 0)
            {
                return new SocialEvent
                {
                    ConnectionId = connection.Id,
                    RawEventId = rawEvent.Id,
                    EventType = SocialEventType.MessageReceived,
                    SchemaVersion = 1,
                    OccurredAt = rawEvent.ReceivedAt,
                    NormalizedPayload = messaging[0].ToString(),
                };
            }

            if (firstEntry.TryGetProperty("changes", out JsonElement changes)
                && changes.ValueKind == JsonValueKind.Array
                && changes.GetArrayLength() > 0)
            {
                JsonElement change = changes[0];
                SocialEventType eventType = change.TryGetProperty("field", out JsonElement field)
                    && string.Equals(field.GetString(), "comments", StringComparison.OrdinalIgnoreCase)
                    ? SocialEventType.CommentCreated
                    : SocialEventType.Unknown;

                return new SocialEvent
                {
                    ConnectionId = connection.Id,
                    RawEventId = rawEvent.Id,
                    EventType = eventType,
                    SchemaVersion = 1,
                    OccurredAt = rawEvent.ReceivedAt,
                    NormalizedPayload = change.ToString(),
                };
            }
        }

        return new SocialEvent
        {
            ConnectionId = connection.Id,
            RawEventId = rawEvent.Id,
            EventType = SocialEventType.Unknown,
            SchemaVersion = 1,
            OccurredAt = rawEvent.ReceivedAt,
            NormalizedPayload = rawEvent.RawBody,
        };
    }

    public async Task<SocialProfile?> FetchProfileAsync(SocialConnection connection, string accessToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        HttpClient httpClient = httpClientFactory.CreateClient(nameof(InstagramSocialProviderAdapter));
        string url = $"https://graph.instagram.com/me?fields=id,username,account_type&access_token={Uri.EscapeDataString(accessToken)}";

        JsonElement? response = await httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
        if (response is not { ValueKind: JsonValueKind.Object } profile)
        {
            return null;
        }

        string externalId = profile.TryGetProperty("id", out JsonElement id) ? id.GetString() ?? connection.ExternalAccountId : connection.ExternalAccountId;
        string? username = profile.TryGetProperty("username", out JsonElement usernameElement) ? usernameElement.GetString() : null;

        return new SocialProfile(externalId, username, username, profile.ToString());
    }
}
