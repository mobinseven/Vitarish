using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface ICartItemService : IEntityService<CartItemReadDto, CartItemWriteDto, CartItemDescriptorDto>;
