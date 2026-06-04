namespace Vitarish.Appointments.Domain.Dto;

public record ShiftReservationReadDto
{
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int TotalSeats { get; set; }
    public int ReservationCount { get; set; }
    public int AvailableSeats { get; set; }
    public TimeSpan? EstimatedSessionLength { get; set; }
    public bool IsClosed { get; set; }

    [JsonIgnore]
    public DateTime SlotStart => DateTime.SpecifyKind(Date.ToDateTime(StartTime), DateTimeKind.Utc);

    [JsonIgnore]
    public DateTime SlotEnd => DateTime.SpecifyKind(Date.ToDateTime(EndTime), DateTimeKind.Utc);
}
