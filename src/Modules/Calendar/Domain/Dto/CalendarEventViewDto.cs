namespace Vitarish.Calendar.Domain.Dto;

public record CalendarEventViewDto
{
    public Guid MappingId { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public string Title { get; init; } = string.Empty;
    public CalendarObjectCategory Category { get; init; }
    public string BusinessStatus { get; init; } = string.Empty;
    public string? ParticipantSummary { get; init; }
    public bool IsRecurring { get; init; }
    public string? RecurrenceId { get; init; }
}
