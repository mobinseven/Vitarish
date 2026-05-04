namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(SessionOccurrence))]
public record SessionOccurrenceDescriptor : EntityDescriptor
{
    public Guid? SessionId { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public bool? IsCancelled { get; init; }
}
