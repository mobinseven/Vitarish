namespace Vitarish.Appointments.Domain.Dto;

public record SessionOccurrenceReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int Capacity { get; set; }
    public bool IsCancelled { get; set; }
    public SessionOccurrenceStats Stats { get; set; } = new();

    [JsonIgnore]
    public DateTimeOffset OccurrenceStart => new(Date.ToDateTime(StartTime), TimeSpan.Zero);

    [JsonIgnore]
    public DateTimeOffset OccurrenceEnd => new(Date.ToDateTime(EndTime), TimeSpan.Zero);
}
