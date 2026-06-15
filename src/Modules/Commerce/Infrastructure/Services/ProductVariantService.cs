namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class ProductVariantService(IMediator mediator) : EntityService<ProductVariant, ProductVariantDescriptor, ProductVariantReadDto, ProductVariantWriteDto, ProductVariantDescriptorDto>
    (
        mediator,
        variant => variant.ToRead(),
        variant => variant.ToEntity(),
        variant => variant.ToDescriptor()
    ),
    IProductVariantService;
