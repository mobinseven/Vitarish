namespace Vitarish.Commerce.Application.Queries;

public record GetStorefrontProductsQuery(ProductDescriptorDto Descriptor) : IRequest<IReadOnlyList<ProductReadDto>>;
