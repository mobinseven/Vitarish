namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Session))]
public record SessionDescriptor : EntityDescriptor
{
    public SessionStatus? Status { get; init; }
    public SessionVisibility? Visibility { get; init; }
    public DayOfWeek? DayOfWeek { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public SessionSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
}
