namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class ProductService(IMediator mediator) : EntityService<Product, ProductDescriptor, ProductReadDto, ProductWriteDto, ProductDescriptorDto>
    (
        mediator,
        product => product.ToRead(),
        product => product.ToEntity(),
        product => product.ToDescriptor()
    ),
    IProductService;
