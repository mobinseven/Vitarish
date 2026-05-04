namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(DomainCalendarRecord))]
public record DomainCalendarRecordDescriptor : EntityDescriptor
{
    public CalendarObjectCategory? Category { get; init; }
    public CalendarObjectVisibility? Visibility { get; init; }
    public string? OwnerId { get; init; }
    public string? ProviderId { get; init; }
    public string? PatientId { get; init; }
    public string? BusinessStatus { get; init; }
}
