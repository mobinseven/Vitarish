namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Reservation))]
public record ReservationDescriptor : EntityDescriptor
{
    public ReservationStatus? Status { get; init; }
    public Guid? ShiftId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string? ReservingPartyId { get; init; }
    public bool? IsHold { get; init; }
}
