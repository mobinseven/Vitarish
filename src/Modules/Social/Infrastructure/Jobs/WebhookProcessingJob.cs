namespace Vitarish.Social.Infrastructure.Jobs;

[RegisterTransient]
internal sealed class WebhookProcessingJob(
    ILogger<WebhookProcessingJob> logger,
    IEntityRepository<RawWebhookEvent, RawWebhookEventDescriptor> rawWebhookRepository,
    IEntityRepository<SocialConnection, SocialConnectionDescriptor> connectionRepository,
    IEntityRepository<SocialEvent, SocialEventDescriptor> socialEventRepository,
    ISocialProviderAdapterRegistry adapterRegistry)
{
    public async Task ProcessRawEventAsync(Guid rawEventId, CancellationToken cancellationToken)
    {
        RawWebhookEvent? rawEvent = await rawWebhookRepository.GetAsync(rawEventId, cancellationToken);
        if (rawEvent is null)
        {
            return;
        }

        if (rawEvent.ConnectionId is null)
        {
            rawEvent.ProcessingStatus = WebhookProcessingStatus.DeadLetter;
            rawEvent.ProcessingError = "Raw event has no resolved social connection.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return;
        }

        SocialConnection? connection = await connectionRepository.GetAsync(rawEvent.ConnectionId.Value, cancellationToken);
        if (connection is null)
        {
            rawEvent.ProcessingStatus = WebhookProcessingStatus.DeadLetter;
            rawEvent.ProcessingError = "Social connection not found.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return;
        }

        if (!adapterRegistry.TryResolve(connection.Provider, out ISocialProviderAdapter adapter))
        {
            rawEvent.ProcessingStatus = WebhookProcessingStatus.DeadLetter;
            rawEvent.ProcessingError = $"No adapter registered for provider {connection.Provider}.";
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            return;
        }

        rawEvent.ProcessingStatus = WebhookProcessingStatus.Processing;
        _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);

        try
        {
            SocialEvent? normalized = adapter.ParseWebhook(connection, rawEvent);
            if (normalized is not null)
            {
                _ = await socialEventRepository.AddAsync(normalized, cancellationToken);
            }

            rawEvent.ProcessingStatus = WebhookProcessingStatus.Processed;
            rawEvent.ProcessingError = null;
            rawEvent.ProcessedAt = DateTimeOffset.UtcNow;
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process social webhook raw event {RawEventId}", rawEventId);

            rawEvent.RetryCount += 1;
            rawEvent.ProcessingError = ex.Message;

            if (rawEvent.RetryCount >= 3)
            {
                rawEvent.ProcessingStatus = WebhookProcessingStatus.DeadLetter;
                rawEvent.ProcessedAt = DateTimeOffset.UtcNow;
                _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
                return;
            }

            rawEvent.ProcessingStatus = WebhookProcessingStatus.Failed;
            _ = await rawWebhookRepository.UpdateAsync(rawEvent, cancellationToken);
            throw;
        }
    }
}
