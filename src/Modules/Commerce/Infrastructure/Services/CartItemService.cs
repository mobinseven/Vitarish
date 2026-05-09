namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CartItemService(IMediator mediator) : EntityService<CartItem, CartItemDescriptor, CartItemReadDto, CartItemWriteDto, CartItemDescriptorDto>
    (
        mediator,
        item => item.ToDto(),
        item => item.ToDomain(),
        item => item.ToDomain()
    ),
    ICartItemService;
