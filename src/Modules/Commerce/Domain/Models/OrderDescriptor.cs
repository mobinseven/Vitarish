namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(Order))]
public record OrderDescriptor : EntityDescriptor
{
    public int? OrderNumber { get; init; }
    public string? CustomerId { get; init; }
    public OrderStatus? Status { get; init; }
}
