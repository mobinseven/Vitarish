namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(AvailabilityPolicy))]
public record AvailabilityPolicyDescriptor : EntityDescriptor
{
    public Guid? CalendarAccountId { get; init; }
    public CalendarObjectCategory? Category { get; init; }
}
