using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface IProductVariantService : IEntityService<ProductVariantReadDto, ProductVariantWriteDto, ProductVariantDescriptorDto>;
