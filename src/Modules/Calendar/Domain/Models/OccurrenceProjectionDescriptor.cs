namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(OccurrenceProjection))]
public record OccurrenceProjectionDescriptor : EntityDescriptor
{
    public Guid? MappingId { get; init; }
    public DateTimeOffset? RangeStart { get; init; }
    public DateTimeOffset? RangeEnd { get; init; }
    public bool? IsCancelled { get; init; }
}
