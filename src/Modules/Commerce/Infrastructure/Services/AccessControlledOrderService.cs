using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Commerce.Infrastructure.Services;

internal class AccessControlledOrderService(
    IRequestContext requestContext,
    IOrderService orderService) : IAccessControlledOrderService
{
    private bool ElevatedAccess() => requestContext.IsPlatformAdmin() || requestContext.IsTenantOwner();

    private static Exception ElevatedAccessRequired => new InvalidOperationException("Elevated access is required to perform this operation");

    private OrderDescriptorDto ScopeToCustomer(OrderDescriptorDto descriptor)
    {
        if (ElevatedAccess())
        {
            return descriptor;
        }
        return descriptor with { CustomerId = requestContext.UserId };
    }

    public Task<EntityResult<OrderReadDto>> AddAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.AddAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> AddAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.AddAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<OrderReadDto>> UpdateAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.UpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> UpdateAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.UpdateAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<OrderReadDto>> AddOrUpdateAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.AddOrUpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.RemoveAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.RemoveAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.RemoveAsync(id, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<OrderReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => orderService.GetAsync(id, cancellationToken);

    public Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.CountAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.GetListAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult<PartedResult<OrderReadDto>>> GetPartedAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.GetPartedAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult> CancelAsync(Guid orderId, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.CancelAsync(orderId, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken)
        => ElevatedAccess() ? orderService.RequestRefundAsync(orderId, note, cancellationToken) : throw ElevatedAccessRequired;
}
