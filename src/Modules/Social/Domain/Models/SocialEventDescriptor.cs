namespace Vitarish.Social.Domain.Models;

[EntityDescriptorFor(typeof(SocialEvent))]
public record SocialEventDescriptor : EntityDescriptor
{
    public Guid? ConnectionId { get; init; }
    public Guid? RawEventId { get; init; }
    public SocialEventType? EventType { get; init; }
}
