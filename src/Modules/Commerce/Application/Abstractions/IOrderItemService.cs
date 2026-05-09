using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Commerce.Application.Abstractions;

public interface IOrderItemService : IEntityService<OrderItemReadDto, OrderItemWriteDto, OrderItemDescriptorDto>;
