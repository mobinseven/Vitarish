namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class ProductVariantService(IMediator mediator) : EntityService<ProductVariant, ProductVariantDescriptor, ProductVariantReadDto, ProductVariantWriteDto, ProductVariantDescriptorDto>
    (
        mediator,
        variant => variant.ToDto(),
        variant => variant.ToDomain(),
        variant => variant.ToDomain()
    ),
    IProductVariantService;
