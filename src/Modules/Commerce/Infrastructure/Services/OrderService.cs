namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class OrderService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<Order, OrderDescriptor, OrderReadDto, OrderWriteDto, OrderDescriptorDto>
    (
        mediator,
        order => order.ToRead(),
        order => order.ToEntity(),
        order => order.ToDescriptor()
    ),
    IOrderService
{
    public new async Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = dbContext.Orders.AsNoTracking();

        if (descriptor.Id.HasValue)
        {
            query = query.Where(o => o.Id == descriptor.Id.Value);
        }

        if (descriptor.OrderNumber.HasValue)
        {
            query = query.Where(o => o.OrderNumber == descriptor.OrderNumber.Value);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.CustomerId))
        {
            query = query.Where(o => o.CustomerId == descriptor.CustomerId);
        }

        if (descriptor.Status.HasValue)
        {
            query = query.Where(o => o.Status == descriptor.Status.Value);
        }

        if (descriptor.CreatedFrom.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= descriptor.CreatedFrom.Value);
        }

        if (descriptor.CreatedTo.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= descriptor.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            string term = descriptor.Search.Trim();
            query = query.Where(o =>
                (o.CustomerId != null && o.CustomerId.Contains(term))
                || (o.MedusaId != null && o.MedusaId.Contains(term))
                || o.OrderNumber.ToString().Contains(term));
        }

        query = query
            .OrderByDescending(o => o.OrderNumber)
            .ThenByDescending(o => o.CreatedAt);

        if (descriptor.Skip.HasValue)
        {
            query = query.Skip(descriptor.Skip.Value);
        }

        if (descriptor.Take.HasValue)
        {
            query = query.Take(descriptor.Take.Value);
        }

        List<Order> orders = await query.ToListAsync(cancellationToken);
        Dictionary<string, string> profileMap = await dbContext.CustomerProfiles
            .AsNoTracking()
            .Where(profile => !string.IsNullOrWhiteSpace(profile.MedusaCustomerId) && !string.IsNullOrWhiteSpace(profile.UserId))
            .ToDictionaryAsync(p => p.MedusaCustomerId, p => p.UserId, cancellationToken);

        IReadOnlyList<OrderReadDto> result = [.. orders.Select(order =>
        {
            OrderReadDto dto = order.ToRead();
            dto.CustomerDisplay = ResolveCustomerDisplay(dto.CustomerId, profileMap);
            return dto;
        })];

        return EntityResult<IReadOnlyList<OrderReadDto>>.Success(result);
    }

    public async Task<EntityResult> CancelAsync(Guid orderId, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
        if (order is null)
        {
            return EntityResult.Failure();
        }

        order.Status = OrderStatus.Canceled;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult.Success();
    }

    public async Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken)
    {
        Order? order = await dbContext.Orders.FindAsync([orderId], cancellationToken);
        if (order is null)
        {
            return EntityResult.Failure();
        }

        order.RefundStatus = RefundStatus.Requested;
        if (!string.IsNullOrWhiteSpace(note))
        {
            order.RefundNote = note.Trim();
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return EntityResult.Success();
    }

    private static string ResolveCustomerDisplay(string customerId, IReadOnlyDictionary<string, string> profileMap) => string.IsNullOrWhiteSpace(customerId)
            ? "Guest checkout"
            : profileMap.TryGetValue(customerId, out string? userId) && !string.IsNullOrWhiteSpace(userId)
            ? $"Registered • {userId}"
            : "Registered customer";
}
