namespace Vitarish.Appointments.Domain.Models;

public sealed class SessionStats
{
    public Guid SessionId { get; init; }
    public int ReservationCount { get; init; }
}
