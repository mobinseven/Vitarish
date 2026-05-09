namespace Vitarish.Commerce.Domain.Mappers;

public partial class EntityMapper
{
    public partial global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto ToDto(global::Vitarish.Commerce.Domain.Entities.ProductVariant src);

    public partial global::Vitarish.Commerce.Domain.Entities.ProductVariant ToDomain(global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto src);

    public partial global::Vitarish.Commerce.Domain.Dto.ProductVariantWriteDto ToWrite(global::Vitarish.Commerce.Domain.Dto.ProductVariantReadDto src);

    public partial global::Vitarish.Commerce.Domain.Models.ProductVariantDescriptor ToDomain(global::Vitarish.Commerce.Domain.Dto.ProductVariantDescriptorDto src);
}
