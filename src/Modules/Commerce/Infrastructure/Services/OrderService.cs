namespace Vitarish.Commerce.Infrastructure.Services;

[RegisterTransient]
internal class OrderService(
    IMediator mediator,
    CommerceDbContext dbContext) : EntityService<Order, OrderDescriptor, OrderReadDto, OrderWriteDto, OrderDescriptorDto>
    (
        mediator,
        order => order.ToDto(),
        order => order.ToDomain(),
        order => order.ToDomain()
    ),
    IOrderService
{
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
}
