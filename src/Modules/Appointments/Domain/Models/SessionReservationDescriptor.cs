namespace Vitarish.Appointments.Domain.Models;

public record SessionReservationDescriptor
{
    public Guid? SessionId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public bool? HasAvailableSeats { get; init; }
}
