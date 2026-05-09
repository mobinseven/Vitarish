namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(OrderItem))]
public record OrderItemDescriptor : EntityDescriptor
{
    public Guid? OrderId { get; init; }
}
