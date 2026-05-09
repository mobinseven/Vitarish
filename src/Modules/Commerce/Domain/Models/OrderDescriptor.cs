namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(Order))]
public record OrderDescriptor : EntityDescriptor
{
    public string? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
}
