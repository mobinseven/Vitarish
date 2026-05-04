namespace Vitarish.Calendar.Domain.Dto;

public record OccurrenceProjectionReadDto : EntityReadDto
{
    public Guid MappingId { get; set; }
    public string RecurrenceId { get; set; } = string.Empty;
    public DateTimeOffset OccurrenceStart { get; set; }
    public DateTimeOffset OccurrenceEnd { get; set; }
    public bool IsException { get; set; }
    public bool IsCancelled { get; set; }
    public DateTimeOffset ProjectedAt { get; set; }
    public DateTimeOffset HorizonEnd { get; set; }
}
