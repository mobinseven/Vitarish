namespace Vitarish.Calendar.Domain.Dto;

public record CalendarSyncLedgerReadDto : EntityReadDto
{
    public Guid CalendarAccountId { get; set; }
    public SyncDirection SyncDirection { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int ItemsProcessed { get; set; }
    public int Conflicts { get; set; }
    public int Failures { get; set; }
    public string? LastError { get; set; }
    public SyncStatus Status { get; set; }
}
