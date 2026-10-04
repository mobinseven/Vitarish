namespace Vitarish.Audit.Domain;

public sealed record SecurityAuditEventReadDto
{
    public Guid Id { get; init; }
    public string EventName { get; init; } = string.Empty;
    public string? EventCategory { get; init; }
    public string Outcome { get; init; } = string.Empty;
    public string? ActorKind { get; init; }
    public string? ActorId { get; init; }
    public string? SubjectId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string? Resource { get; init; }
    public string Operation { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public string SourceBoundary { get; init; } = string.Empty;
    public string? SourceIpKind { get; init; }
    public string? SourceIpAddress { get; init; }
    public string? ClientInterfaceHost { get; init; }
    public string? RedactedReason { get; init; }
}
