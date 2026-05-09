namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class OrderItemService(IMediator mediator) : EntityService<OrderItem, OrderItemDescriptor, OrderItemReadDto, OrderItemWriteDto, OrderItemDescriptorDto>
    (
        mediator,
        item => item.ToDto(),
        item => item.ToDomain(),
        item => item.ToDomain()
    ),
    IOrderItemService;
