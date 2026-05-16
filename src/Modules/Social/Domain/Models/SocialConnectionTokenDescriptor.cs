namespace Vitarish.Social.Domain.Models;

[EntityDescriptorFor(typeof(SocialConnectionToken))]
public record SocialConnectionTokenDescriptor : EntityDescriptor
{
    public Guid? ConnectionId { get; init; }
}
