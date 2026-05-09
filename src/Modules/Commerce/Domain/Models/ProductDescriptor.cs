namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(Product))]
public record ProductDescriptor : EntityDescriptor
{
    public ProductStatus? Status { get; init; }
    public Guid? CategoryId { get; init; }
}
