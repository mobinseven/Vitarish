namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class CartService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<Cart, CartDescriptor, CartReadDto, CartWriteDto, CartDescriptorDto>
    (
        mediator,
        cart => cart.ToDto(),
        cart => cart.ToDomain(),
        cart => cart.ToDomain()
    ),
    ICartService
{
    public async Task<CartReadDto?> GetActiveCartAsync(string customerId, CancellationToken cancellationToken)
    {
        Cart? cart = await dbContext.Carts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId && c.Status == CartStatus.Open)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        return cart?.ToDto();
    }

    public async Task<EntityResult<CartReadDto>> InitiateCheckoutAsync(Guid cartId, CancellationToken cancellationToken)
    {
        Cart? cart = await dbContext.Carts.FindAsync([cartId], cancellationToken);
        if (cart is null)
        {
            return EntityResult<CartReadDto>.Failure();
        }

        cart.Status = CartStatus.Ordered;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult<CartReadDto>.Success(cart.ToDto());
    }
}
