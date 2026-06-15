namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CartItemService(IMediator mediator) : EntityService<CartItem, CartItemDescriptor, CartItemReadDto, CartItemWriteDto, CartItemDescriptorDto>
    (
        mediator,
        item => item.ToRead(),
        item => item.ToEntity(),
        item => item.ToDescriptor()
    ),
    ICartItemService;
