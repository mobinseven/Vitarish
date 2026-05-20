namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(Shift))]
public record ShiftDescriptor : EntityDescriptor
{
    public ShiftStatus? Status { get; init; }
    public ShiftVisibility? Visibility { get; init; }
    public DayOfWeek? DayOfWeek { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }
    public ShiftSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
    public Guid? ServiceProviderId { get; init; }
    public Guid? ServiceId { get; init; }
}
