namespace Vitarish.Audit.Domain;

/// <summary>
/// A minimal, attributable security event. Payloads, credentials, tokens, and
/// request bodies are deliberately not part of this durable record.
/// </summary>
public sealed class SecurityAuditEvent
{
    public Guid Id { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? EventCategory { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public string? ActorKind { get; set; }
    public string? ActorId { get; set; }
    public string? SubjectId { get; set; }
    public SecurityAuditEventMetadata? Metadata { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string? Resource { get; set; }
    public string Operation { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string SourceBoundary { get; set; } = string.Empty;
    public string? SourceIpKind { get; set; }
    public string? SourceIpAddress { get; set; }
    public string? RedactedReason { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
