namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class OrderItemService(IMediator mediator) : EntityService<OrderItem, OrderItemDescriptor, OrderItemReadDto, OrderItemWriteDto, OrderItemDescriptorDto>
    (
        mediator,
        item => item.ToRead(),
        item => item.ToEntity(),
        item => item.ToDescriptor()
    ),
    IOrderItemService;
