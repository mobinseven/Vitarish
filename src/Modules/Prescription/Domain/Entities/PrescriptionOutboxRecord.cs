namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Persisted outbox record for deferred/retryable prescription provider operations.
/// Mirrors the Platform OutboxEntry contract, scoped to the Prescription schema.
/// </summary>
public sealed class PrescriptionOutboxRecord
{
    public Guid Id { get; set; }
    public Guid? PrescriptionId { get; set; }
    public required string ProviderKey { get; set; }
    public required string OperationName { get; set; }
    public required string PayloadJson { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string CorrelationId { get; set; }
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 3;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? NextRetryAt { get; set; }
    public DateTimeOffset? LastAttemptAt { get; set; }
    public string? LastError { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
