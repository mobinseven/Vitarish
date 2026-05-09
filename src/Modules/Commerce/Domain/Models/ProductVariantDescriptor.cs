namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(ProductVariant))]
public record ProductVariantDescriptor : EntityDescriptor
{
    public Guid? ProductId { get; init; }
}
