using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Vitarish.Social.Infrastructure.Adapters;

[RegisterTransient]
internal sealed class TelegramSocialProviderAdapter(IHttpClientFactory httpClientFactory) : ISocialProviderAdapter
{
    private static readonly IReadOnlySet<SocialCapability> AdapterCapabilities = new HashSet<SocialCapability>
    {
        SocialCapability.WebhookInbound,
        SocialCapability.FetchProfile,
        SocialCapability.FetchMessages,
    };

    public SocialProvider Provider => SocialProvider.Telegram;

    public IReadOnlySet<SocialCapability> Capabilities => AdapterCapabilities;

    public bool VerifyWebhook(IReadOnlyDictionary<string, string> headers, string body, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return false;
        }

        if (!headers.TryGetValue("X-Telegram-Signature", out string? signatureHeader) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        byte[] key = Encoding.UTF8.GetBytes(secret);
        byte[] payload = Encoding.UTF8.GetBytes(body);
        using HMACSHA256 hmac = new(key);
        byte[] expectedHash = hmac.ComputeHash(payload);

        return TryDecodeSignature(signatureHeader, expectedHash.Length, out byte[]? providedHash)
            && CryptographicOperations.FixedTimeEquals(expectedHash, providedHash);
    }

    private static bool TryDecodeSignature(string signatureHeader, int hashByteLength, out byte[]? providedHash)
    {
        providedHash = null;
        string providedHex = signatureHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? signatureHeader[7..]
            : signatureHeader;

        if (providedHex.Length != hashByteLength * 2)
        {
            return false;
        }

        try
        {
            providedHash = Convert.FromHexString(providedHex);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public SocialEvent? ParseWebhook(SocialConnection connection, RawWebhookEvent rawEvent)
    {
        using JsonDocument doc = JsonDocument.Parse(rawEvent.RawBody);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("message", out JsonElement message))
        {
            string payload = JsonSerializer.Serialize(new
            {
                MessageId = message.TryGetProperty("message_id", out JsonElement messageId) ? messageId.GetInt64() : 0,
                Text = message.TryGetProperty("text", out JsonElement text) ? text.GetString() : null,
                From = message.TryGetProperty("from", out JsonElement from) ? from.ToString() : null,
                Chat = message.TryGetProperty("chat", out JsonElement chat) ? chat.ToString() : null,
            });

            return new SocialEvent
            {
                ConnectionId = connection.Id,
                RawEventId = rawEvent.Id,
                EventType = SocialEventType.MessageReceived,
                SchemaVersion = 1,
                OccurredAt = rawEvent.ReceivedAt,
                NormalizedPayload = payload,
            };
        }

        if (root.TryGetProperty("callback_query", out JsonElement callback))
        {
            string payload = JsonSerializer.Serialize(new
            {
                CallbackId = callback.TryGetProperty("id", out JsonElement id) ? id.GetString() : null,
                Data = callback.TryGetProperty("data", out JsonElement data) ? data.GetString() : null,
            });

            return new SocialEvent
            {
                ConnectionId = connection.Id,
                RawEventId = rawEvent.Id,
                EventType = SocialEventType.MessageReceived,
                SchemaVersion = 1,
                OccurredAt = rawEvent.ReceivedAt,
                NormalizedPayload = payload,
            };
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

        HttpClient httpClient = httpClientFactory.CreateClient(nameof(TelegramSocialProviderAdapter));
        string url = $"https://api.telegram.org/bot{Uri.EscapeDataString(accessToken)}/getMe";

        JsonElement? response = await httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
        if (response is not { ValueKind: JsonValueKind.Object } value)
        {
            return null;
        }

        if (!value.TryGetProperty("result", out JsonElement result))
        {
            return null;
        }

        string externalId = result.TryGetProperty("id", out JsonElement id) ? id.ToString() : connection.ExternalAccountId;
        string? username = result.TryGetProperty("username", out JsonElement usernameElement) ? usernameElement.GetString() : null;
        string? displayName = result.TryGetProperty("first_name", out JsonElement firstName) ? firstName.GetString() : null;

        return new SocialProfile(externalId, username, displayName, result.ToString());
    }
}
