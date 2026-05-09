using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Commerce.Infrastructure.Services;

internal class AccessControlledOrderService(
    IRequestContext requestContext,
    IOrderService orderService) : IAccessControlledOrderService
{
    private bool IsAdmin() => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static Exception AdminRoleRequired => new InvalidOperationException("Admin role is required to perform this operation");

    private OrderDescriptorDto ScopeToCustomer(OrderDescriptorDto descriptor)
    {
        if (IsAdmin())
        {
            return descriptor;
        }
        return descriptor with { CustomerId = requestContext.UserId };
    }

    public Task<EntityResult<OrderReadDto>> AddAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> AddAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<OrderReadDto>> UpdateAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> UpdateAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<OrderReadDto>> AddOrUpdateAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(OrderWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<OrderWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<OrderReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => orderService.GetAsync(id, cancellationToken);

    public Task<EntityResult<int>> CountAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.CountAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult<IReadOnlyList<OrderReadDto>>> GetListAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.GetListAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult<PartedResult<OrderReadDto>>> GetPartedAsync(OrderDescriptorDto descriptor, CancellationToken cancellationToken)
        => orderService.GetPartedAsync(ScopeToCustomer(descriptor), cancellationToken);

    public Task<EntityResult> CancelAsync(Guid orderId, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.CancelAsync(orderId, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RequestRefundAsync(Guid orderId, string? note, CancellationToken cancellationToken)
        => IsAdmin() ? orderService.RequestRefundAsync(orderId, note, cancellationToken) : throw AdminRoleRequired;
}
