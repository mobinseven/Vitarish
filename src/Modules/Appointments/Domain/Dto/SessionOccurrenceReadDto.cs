namespace Vitarish.Appointments.Domain.Dto;

public record SessionOccurrenceReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public Guid CalendarMappingId { get; set; }
    public string RecurrenceId { get; set; } = string.Empty;
    public DateTimeOffset OccurrenceStart { get; set; }
    public DateTimeOffset OccurrenceEnd { get; set; }
    public bool IsCancelled { get; set; }
    public int? CapacityOverride { get; set; }
    public SessionOccurrenceStats Stats { get; set; } = new();
}
