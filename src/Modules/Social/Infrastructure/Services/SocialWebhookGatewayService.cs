using Hangfire;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vitarish.Social.Infrastructure.Jobs;

namespace Vitarish.Social.Infrastructure.Services;

[RegisterTransient]
internal sealed class SocialWebhookGatewayService(
    ILogger<SocialWebhookGatewayService> logger,
    IBackgroundJobClient backgroundJobClient,
    IEntityRepository<SocialConnection, SocialConnectionDescriptor> connectionRepository,
    IEntityRepository<RawWebhookEvent, RawWebhookEventDescriptor> rawWebhookRepository,
    ISocialProviderAdapterRegistry adapterRegistry) : ISocialWebhookGatewayService
{
    public async Task<Guid> IngestAsync(
        SocialProvider provider,
        Guid? connectionId,
        IReadOnlyDictionary<string, string> headers,
        string body,
        CancellationToken cancellationToken)
    {
        string serializedHeaders = JsonSerializer.Serialize(headers);

        RawWebhookEvent rawEvent = new()
        {
            Provider = provider,
            ConnectionId = connectionId,
            RawHeaders = serializedHeaders,
            RawBody = body,
            VerificationStatus = WebhookVerificationStatus.Pending,
            ProcessingStatus = WebhookProcessingStatus.Pending,
            ReceivedAt = DateTime.UtcNow,
            IdempotencyKey = $"pending:{Guid.NewGuid():N}",
        };

        _ = await rawWebhookRepository.AddAsync(rawEvent, cancellationToken);

        SocialConnection? connection = await ResolveConnectionAsync(provider, connectionId, cancellationToken);
        if (connection is null)
        {
            rawEvent.VerificationStatus = WebhookVerificationStatus.Failed;
            rawEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
            rawEvent.ProcessingError = "No matching connection was found for incoming webhook.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return rawEvent.Id;
        }

        rawEvent.ConnectionId = connection.Id;

        if (!adapterRegistry.TryResolve(provider, out ISocialProviderAdapter adapter))
        {
            rawEvent.VerificationStatus = WebhookVerificationStatus.Failed;
            rawEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
            rawEvent.ProcessingError = $"No adapter registered for provider {provider}.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return rawEvent.Id;
        }

        string idempotencyKey = BuildIdempotencyKey(provider, connection.Id, headers, body);
        rawEvent.IdempotencyKey = idempotencyKey;

        RawWebhookEventDescriptor duplicateDescriptor = new()
        {
            IdempotencyKey = idempotencyKey,
            Skip = 0,
            Take = 2,
        };

        IReadOnlyList<RawWebhookEvent> duplicates = await rawWebhookRepository.GetAsync(duplicateDescriptor, cancellationToken);
        if (duplicates.Any(existing => existing.Id != rawEvent.Id))
        {
            rawEvent.VerificationStatus = WebhookVerificationStatus.Verified;
            rawEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
            rawEvent.ProcessedAt = DateTime.UtcNow;
            rawEvent.ProcessingError = "Duplicate webhook event ignored.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return rawEvent.Id;
        }

        bool isVerified = adapter.VerifyWebhook(headers, body, connection.WebhookSecret);
        if (!isVerified)
        {
            rawEvent.VerificationStatus = WebhookVerificationStatus.Failed;
            rawEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
            rawEvent.ProcessingError = "Webhook signature verification failed.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return rawEvent.Id;
        }

        rawEvent.VerificationStatus = WebhookVerificationStatus.Verified;
        rawEvent.ProcessingStatus = WebhookProcessingStatus.Pending;
        rawEvent.ProcessingError = null;
        _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);

        _ = backgroundJobClient.Enqueue<WebhookProcessingJob>(job => job.ProcessRawEventAsync(rawEvent.Id, CancellationToken.None));

        return rawEvent.Id;
    }

    public async Task<SocialWebhookReplayResult> ReplayAsync(Guid rawEventId, CancellationToken cancellationToken)
    {
        RawWebhookEvent? rawEvent = await rawWebhookRepository.GetAsync(rawEventId, cancellationToken);
        if (rawEvent is null)
        {
            return SocialWebhookReplayResult.NotFound();
        }

        if (rawEvent.ProcessingStatus != WebhookProcessingStatus.DeadLetter && rawEvent.ProcessingStatus != WebhookProcessingStatus.Failed)
        {
            return SocialWebhookReplayResult.InvalidState();
        }

        rawEvent.ProcessingStatus = WebhookProcessingStatus.Pending;
        rawEvent.ProcessingError = null;
        _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);

        _ = backgroundJobClient.Enqueue<WebhookProcessingJob>(job => job.ProcessRawEventAsync(rawEvent.Id, CancellationToken.None));
        return SocialWebhookReplayResult.Queued();
    }

    private async Task<SocialConnection?> ResolveConnectionAsync(SocialProvider provider, Guid? connectionId, CancellationToken cancellationToken)
    {
        if (connectionId is { } id)
        {
            return await connectionRepository.GetAsync(id, cancellationToken);
        }

        SocialConnectionDescriptor descriptor = new()
        {
            Provider = provider,
            Status = SocialConnectionStatus.Active,
            Skip = 0,
            Take = 2,
        };

        IReadOnlyList<SocialConnection> connections = await connectionRepository.GetAsync(descriptor, cancellationToken);
        if (connections.Count == 1)
        {
            return connections[0];
        }

        logger.LogWarning("Webhook connection resolution found {Count} active connections for provider {Provider}", connections.Count, provider);
        return null;
    }

    private static string BuildIdempotencyKey(
        SocialProvider provider,
        Guid connectionId,
        IReadOnlyDictionary<string, string> headers,
        string body)
    {
        string? providerEventId = TryExtractProviderEventId(provider, headers, body);
        if (!string.IsNullOrWhiteSpace(providerEventId))
        {
            return $"{provider}:{connectionId:N}:{providerEventId}";
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{provider}:{connectionId:N}:{body}"));
        return $"{provider}:{connectionId:N}:{Convert.ToHexStringLower(hash)}";
    }

    private static string? TryExtractProviderEventId(SocialProvider provider, IReadOnlyDictionary<string, string> headers, string body)
    {
        try
        {
            if (provider == SocialProvider.Telegram)
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("update_id", out JsonElement updateId)
                    ? updateId.ToString()
                    : headers.TryGetValue("X-Telegram-Bot-Api-Update-Id", out string? headerId) && !string.IsNullOrWhiteSpace(headerId)
                    ? headerId
                    : null;
            }

            if (provider == SocialProvider.Instagram)
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("entry", out JsonElement entry)
                    && entry.ValueKind == JsonValueKind.Array
                    && entry.GetArrayLength() > 0)
                {
                    JsonElement first = entry[0];
                    if (first.TryGetProperty("id", out JsonElement id))
                    {
                        return id.ToString();
                    }
                }

                if (headers.TryGetValue("X-Hub-Signature-256", out string? signature) && !string.IsNullOrWhiteSpace(signature))
                {
                    return signature;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
