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

    [JsonIgnore]
    public DateTimeOffset SlotStart => new(Date.ToDateTime(StartTime), TimeSpan.Zero);

    [JsonIgnore]
    public DateTimeOffset SlotEnd => new(Date.ToDateTime(EndTime), TimeSpan.Zero);
}
