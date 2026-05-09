namespace Vitarish.Appointments.Domain.Models;

public record SessionOccurrenceRange
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}
