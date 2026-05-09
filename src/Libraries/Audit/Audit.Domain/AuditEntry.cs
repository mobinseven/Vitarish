namespace Vitarish.Audit.Domain;

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? SnapshotBefore { get; set; }
    public string? SnapshotAfter { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string TenantId { get; set; } = string.Empty;
}
