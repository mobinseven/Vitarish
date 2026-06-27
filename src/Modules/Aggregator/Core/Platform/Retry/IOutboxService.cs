namespace Vitarish.Modules.Aggregator.Core.Platform.Retry;

/// <summary>
/// Centralized outbox service for deferred and retryable operations.
/// Modules enqueue operations; the platform infrastructure handles retry
/// with provider-aware backoff and duplicate-risk detection.
/// </summary>
public interface IOutboxService
{
    /// <summary>
    /// Enqueues an operation for deferred/retried execution.
    /// </summary>
    Task<Guid> EnqueueAsync(OutboxEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entry as completed successfully.
    /// </summary>
    Task CompleteAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entry as failed with error details.
    /// Increments retry count and schedules next retry if within limits.
    /// </summary>
    Task FailAsync(Guid entryId, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an entry as duplicate-risk, requiring manual intervention.
    /// </summary>
    Task MarkDuplicateRiskAsync(Guid entryId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending entries ready for processing (next_retry_at &lt;= now).
    /// </summary>
    Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(string? providerKey = null, int maxItems = 10, CancellationToken cancellationToken = default);
}
