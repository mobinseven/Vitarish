namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(CartItem))]
public record CartItemDescriptor : EntityDescriptor
{
    public Guid? CartId { get; init; }
}
