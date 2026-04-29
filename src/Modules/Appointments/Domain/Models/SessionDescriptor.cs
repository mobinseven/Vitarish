namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Session))]
public record SessionDescriptor : EntityDescriptor
{
    public SessionStatus? Status { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public SessionSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
}
