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
    public DateTime SlotStart => Date.ToDateTime(StartTime);

    [JsonIgnore]
    public DateTime SlotEnd => Date.ToDateTime(EndTime);
}
