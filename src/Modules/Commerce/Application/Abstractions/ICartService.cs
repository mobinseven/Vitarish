using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface ICartService : IEntityService<CartReadDto, CartWriteDto, CartDescriptorDto>
{
    Task<CartReadDto?> GetActiveCartAsync(string customerId, CancellationToken cancellationToken);
    Task<EntityResult<CartReadDto>> InitiateCheckoutAsync(Guid cartId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken);
    Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken);
    Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken);
}
