namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class ProductService(IMediator mediator) : EntityService<Product, ProductDescriptor, ProductReadDto, ProductWriteDto, ProductDescriptorDto>
    (
        mediator,
        product => product.ToDto(),
        product => product.ToDomain(),
        product => product.ToDomain()
    ),
    IProductService;
