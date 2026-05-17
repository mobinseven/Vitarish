namespace Vitarish.Social.Domain.Entities;

public class SocialEvent : Entity
{
    public Guid ConnectionId
    {
        get;
        set => SetField(ref field, value, nameof(ConnectionId));
    }

    public Guid RawEventId
    {
        get;
        set => SetField(ref field, value, nameof(RawEventId));
    }

    public SocialEventType EventType
    {
        get;
        set => SetField(ref field, value, nameof(EventType));
    } = SocialEventType.Unknown;

    public int SchemaVersion
    {
        get;
        set => SetField(ref field, value, nameof(SchemaVersion));
    } = 1;

    public DateTimeOffset OccurredAt
    {
        get;
        set => SetField(ref field, value, nameof(OccurredAt));
    } = DateTimeOffset.UtcNow;

    public string NormalizedPayload
    {
        get;
        set => SetField(ref field, value, nameof(NormalizedPayload));
    } = "{}";
}
