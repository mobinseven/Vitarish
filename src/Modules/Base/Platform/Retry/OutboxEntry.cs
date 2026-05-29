namespace Vitarish.Modules.Base.Platform.Retry;

/// <summary>
/// Represents an outbox entry for a deferred/retryable operation.
/// Modules register operations here for provider-aware retry handling.
/// </summary>
public sealed class OutboxEntry
{
    public Guid Id { get; set; }

    /// <summary>Provider key for provider-specific retry policies.</summary>
    public required string ProviderKey { get; set; }

    /// <summary>Name of the operation to retry.</summary>
    public required string OperationName { get; set; }

    /// <summary>Serialized payload for the operation.</summary>
    public required string PayloadJson { get; set; }

    /// <summary>Idempotency key to prevent duplicates on retry.</summary>
    public required string IdempotencyKey { get; set; }

    /// <summary>Correlation ID linking this entry to the original request.</summary>
    public required string CorrelationId { get; set; }

    /// <summary>Current status: Pending, Processing, Completed, Failed, DuplicateRisk.</summary>
    public OutboxEntryStatus Status { get; set; } = OutboxEntryStatus.Pending;

    /// <summary>Number of retry attempts made.</summary>
    public int RetryCount { get; set; }

    /// <summary>Maximum retries before escalation.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>When this entry was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the next retry should be attempted.</summary>
    public DateTimeOffset? NextRetryAt { get; set; }

    /// <summary>When this entry was last processed.</summary>
    public DateTimeOffset? LastAttemptAt { get; set; }

    /// <summary>Last error message (if failed).</summary>
    public string? LastError { get; set; }

    /// <summary>Tenant context.</summary>
    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Outbox entry lifecycle status.
/// </summary>
public enum OutboxEntryStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    DuplicateRisk,
    Escalated
}
