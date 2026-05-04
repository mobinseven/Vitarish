namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(CalendarAccount))]
public record CalendarAccountDescriptor : EntityDescriptor
{
    public CalendarEntityType? LocalEntityType { get; init; }
    public string? LocalEntityId { get; init; }
    public bool? IsActive { get; init; }
    public SyncStatus? SyncStatus { get; init; }
}
