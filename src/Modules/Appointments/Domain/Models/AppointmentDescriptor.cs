namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Appointment))]
public record AppointmentDescriptor : EntityDescriptor
{
    public AppointmentStatus? Status { get; init; }
    public Guid? SessionId { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public AppointmentSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
}
