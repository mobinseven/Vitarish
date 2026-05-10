namespace Vitarish.Audit.Domain;

public sealed record AuditEntryReadDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? ActorId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public string? SnapshotBefore { get; init; }
    public string? SnapshotAfter { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string TenantId { get; init; } = string.Empty;
}
