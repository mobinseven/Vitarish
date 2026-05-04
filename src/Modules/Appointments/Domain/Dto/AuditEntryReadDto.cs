namespace Vitarish.Appointments.Domain.Dto;

public record AuditEntryReadDto : EntityReadDto
{
    public string Action { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? SnapshotBefore { get; set; }
    public string? SnapshotAfter { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
