namespace Vitarish.Appointments.Domain.Models;

public record ShiftStats
{
    public Guid ShiftId { get; init; }
    public int ReservationCount { get; init; }
}
