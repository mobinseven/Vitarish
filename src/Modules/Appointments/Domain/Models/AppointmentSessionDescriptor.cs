namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(AppointmentSession))]
public record AppointmentSessionDescriptor : EntityDescriptor
{
    public AppointmentSessionStatus? Status { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public AppointmentSessionSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
}
