namespace Vitarish.Appointments.Contracts.Dto;

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
    public DateTime SlotEnd => DateTime.SpecifyKind(
        EndTime >= StartTime
            ? Date.ToDateTime(EndTime)
            : Date.AddDays(1).ToDateTime(EndTime),
        DateTimeKind.Utc);
}
