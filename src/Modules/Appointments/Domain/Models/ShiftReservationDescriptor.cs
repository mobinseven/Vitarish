namespace Vitarish.Appointments.Domain.Models;

public record ShiftReservationDescriptor
{
    public Guid? ShiftId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public bool? HasAvailableSeats { get; init; }
}
