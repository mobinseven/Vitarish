using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Elasticsearch.Application.Abstractions;
using Vitarish.Modules.Elasticsearch.Application.Models;
using Vitarish.Modules.Elasticsearch.Infrastructure.Configuration;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public sealed class EntityIndexingProcessor(
    IEntityIndexingMessageQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<ElasticsearchOptions> options,
    ILogger<EntityIndexingProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (EntityIndexingMessage message in queue.ReadAllAsync(stoppingToken))
        {
            await ProcessWithRetryAsync(message, stoppingToken);
        }
    }

    private async Task ProcessWithRetryAsync(EntityIndexingMessage message, CancellationToken cancellationToken)
    {
        ElasticsearchIndexingProcessorOptions processorOptions = options.Value.IndexingProcessor;

        for (int attempt = 1; attempt <= processorOptions.MaxAttempts; attempt++)
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                ISearchIndexService service = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();

                switch (message)
                {
                    case EntityIndexed indexed:
                        await service.IndexDocumentAsync(new IndexDocumentRequest
                        {
                            TenantId = indexed.TenantId,
                            EntityType = indexed.EntityType,
                            EntityId = indexed.EntityId,
                            Payload = indexed.Payload,
                            Timestamp = indexed.OccurredAt
                        }, cancellationToken);
                        break;

                    case EntityDeleted deleted:
                        await service.DeleteDocumentAsync(deleted.EntityType, deleted.EntityId, deleted.TenantId, cancellationToken);
                        break;
                }

                return;
            }
            catch (Exception ex) when (attempt < processorOptions.MaxAttempts)
            {
                TimeSpan backoff = ComputeBackoff(attempt, processorOptions);
                logger.LogWarning(ex,
                    "Failed to process indexing message {MessageType} {EntityType}/{EntityId} for tenant {TenantId} on attempt {Attempt}/{MaxAttempts}. Retrying in {DelayMs}ms.",
                    message.GetType().Name,
                    message.EntityType,
                    message.EntityId,
                    message.TenantId,
                    attempt,
                    processorOptions.MaxAttempts,
                    backoff.TotalMilliseconds);

                await Task.Delay(backoff, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to process indexing message {MessageType} {EntityType}/{EntityId} for tenant {TenantId} after {MaxAttempts} attempts.",
                    message.GetType().Name,
                    message.EntityType,
                    message.EntityId,
                    message.TenantId,
                    processorOptions.MaxAttempts);
                return;
            }
        }
    }

    private static TimeSpan ComputeBackoff(int attempt, ElasticsearchIndexingProcessorOptions options)
    {
        double exponential = Math.Pow(2, attempt - 1);
        double delayMs = options.BaseBackoff.TotalMilliseconds * exponential;
        return TimeSpan.FromMilliseconds(Math.Min(delayMs, options.MaxBackoff.TotalMilliseconds));
    }
}
