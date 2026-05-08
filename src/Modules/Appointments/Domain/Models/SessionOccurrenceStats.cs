namespace Vitarish.Appointments.Domain.Models;

public record SessionOccurrenceStats
{
    public Guid SessionOccurrenceId { get; init; }
    public int ReservationCount { get; init; }
}
