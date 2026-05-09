using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface IOrderService : IEntityService<OrderReadDto, OrderWriteDto, OrderDescriptorDto>
{
    Task<EntityResult> CancelAsync(Guid orderId, CancellationToken cancellationToken);
}
