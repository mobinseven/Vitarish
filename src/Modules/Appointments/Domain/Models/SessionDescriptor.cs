namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Session))]
public record SessionDescriptor : EntityDescriptor
{
    public SessionStatus? Status { get; init; }
    public SessionVisibility? Visibility { get; init; }
    public string? HostUserId { get; init; }
    public Guid? CalendarMappingId { get; init; }
    public SessionSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
}
