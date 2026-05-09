using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface IProductService : IEntityService<ProductReadDto, ProductWriteDto, ProductDescriptorDto>;
