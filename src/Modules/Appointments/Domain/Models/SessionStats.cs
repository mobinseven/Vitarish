namespace Vitarish.Appointments.Domain.Models;

public record SessionStats
{
    public Guid SessionId { get; init; }
    public int ReservationCount { get; init; }
}
