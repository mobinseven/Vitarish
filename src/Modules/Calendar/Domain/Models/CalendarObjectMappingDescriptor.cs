namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(CalendarObjectMapping))]
public record CalendarObjectMappingDescriptor : EntityDescriptor
{
    public Guid? CalendarAccountId { get; init; }
    public CalendarObjectCategory? Category { get; init; }
    public string? ICalendarUid { get; init; }
    public bool? IsExternallyManaged { get; init; }
}
