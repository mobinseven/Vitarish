namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(CustomerProfile))]
public record CustomerProfileDescriptor : EntityDescriptor
{
    public string? UserId { get; init; }
}
