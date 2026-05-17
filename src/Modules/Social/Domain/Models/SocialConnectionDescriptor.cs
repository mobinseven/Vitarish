namespace Vitarish.Social.Domain.Models;

[EntityDescriptorFor(typeof(SocialConnection))]
public record SocialConnectionDescriptor : EntityDescriptor
{
    public SocialProvider? Provider { get; init; }
    public SocialConnectionStatus? Status { get; init; }
    public string? ExternalAccountId { get; init; }
}
