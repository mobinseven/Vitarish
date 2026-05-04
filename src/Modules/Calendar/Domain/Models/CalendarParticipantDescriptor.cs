namespace Vitarish.Calendar.Domain.Models;

[EntityDescriptorFor(typeof(CalendarParticipant))]
public record CalendarParticipantDescriptor : EntityDescriptor
{
    public Guid? MappingId { get; init; }
    public ParticipantType? ParticipantType { get; init; }
}
