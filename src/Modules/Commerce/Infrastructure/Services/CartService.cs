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
    private sealed record RecoveryQueueCandidate(
        Guid CartId,
        string CustomerId,
        string UserId,
        CartRecoveryStatus RecoveryStatus,
        string RecoveryNote,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastActivityAt);

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

    public async Task<IReadOnlyList<MerchantRecoveryCartReadDto>> GetRecoveryQueueAsync(CancellationToken cancellationToken)
    {
        List<RecoveryQueueCandidate> items = await BuildRecoveryQueueQuery()
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return [];
        }

        List<Guid> cartIds = [.. items.Select(i => i.CartId)];
        Dictionary<Guid, decimal> totals = await dbContext.CartItems
            .AsNoTracking()
            .Where(ci => cartIds.Contains(ci.CartId))
            .GroupBy(ci => ci.CartId)
            .Select(group => new { group.Key, Total = group.Sum(ci => ci.UnitPrice * ci.Quantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, cancellationToken);

        return [.. items
            .OrderByDescending(i => i.LastActivityAt)
            .Select(i => new MerchantRecoveryCartReadDto
            {
                CartId = i.CartId,
                CustomerId = i.CustomerId,
                CustomerDisplay = string.IsNullOrWhiteSpace(i.UserId) ? "Registered customer" : $"User • {i.UserId}",
                HasContact = !string.IsNullOrWhiteSpace(i.UserId),
                EstimatedValue = totals.GetValueOrDefault(i.CartId, 0m),
                CreatedAt = i.CreatedAt,
                LastActivityAt = i.LastActivityAt,
                RecoveryStatus = i.RecoveryStatus,
                RecoveryNote = i.RecoveryNote
            })];
    }

    public async Task<int> CountRecoveryQueueAsync(CancellationToken cancellationToken) => await BuildRecoveryQueueQuery()
            .Where(item => item.RecoveryStatus != CartRecoveryStatus.Recovered
                && item.RecoveryStatus != CartRecoveryStatus.Ignored)
            .Select(item => item.CartId)
            .Distinct()
            .CountAsync(cancellationToken);

    public async Task<EntityResult> UpdateRecoveryAsync(Guid cartId, CartRecoveryStatus status, string? note, CancellationToken cancellationToken)
    {
        Cart? cart = await dbContext.Carts.FindAsync([cartId], cancellationToken);
        if (cart is null)
        {
            return EntityResult.Failure();
        }

        cart.RecoveryStatus = status;
        cart.RecoveryNote = note?.Trim() ?? string.Empty;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult.Success();
    }

    private IQueryable<RecoveryQueueCandidate> BuildRecoveryQueueQuery() => from cart in dbContext.Carts.AsNoTracking()
                                                                            join profile in dbContext.CustomerProfiles.AsNoTracking() on cart.CustomerId equals profile.MedusaCustomerId
                                                                            where cart.Status == CartStatus.Abandoned
                                                                                && !string.IsNullOrWhiteSpace(profile.UserId)
                                                                            select new RecoveryQueueCandidate(
                                                                                cart.Id,
                                                                                cart.CustomerId,
                                                                                profile.UserId,
                                                                                cart.RecoveryStatus,
                                                                                cart.RecoveryNote,
                                                                                cart.CreatedAt,
                                                                                cart.UpdatedAt ?? cart.CreatedAt);
}
