namespace Vitarish.Calendar.Domain.Entities;

public class CalendarParticipant : Entity
{
    public Guid MappingId
    {
        get;
        set => SetField(ref field, value, nameof(MappingId));
    }

    public ParticipantType ParticipantType
    {
        get;
        set => SetField(ref field, value, nameof(ParticipantType));
    }

    public string LocalReferenceId
    {
        get;
        set => SetField(ref field, value, nameof(LocalReferenceId));
    } = string.Empty;

    public ParticipantRole Role
    {
        get;
        set => SetField(ref field, value, nameof(Role));
    } = ParticipantRole.Attendee;
}
