namespace Vitarish.Appointments.Domain.Models;

[EntityDescriptorFor(typeof(SessionOccurrence))]
public record SessionOccurrenceDescriptor : EntityDescriptor
{
    public Guid? SessionId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public bool? IsCancelled { get; init; }
}
