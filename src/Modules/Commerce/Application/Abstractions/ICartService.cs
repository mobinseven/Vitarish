using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface ICartService : IEntityService<CartReadDto, CartWriteDto, CartDescriptorDto>
{
    Task<CartReadDto?> GetActiveCartAsync(string customerId, CancellationToken cancellationToken);
    Task<EntityResult<CartReadDto>> InitiateCheckoutAsync(Guid cartId, CancellationToken cancellationToken);
}
