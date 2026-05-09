using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Commerce.Infrastructure.Services;

internal class AccessControlledProductService(
    IRequestContext requestContext,
    IProductService productService) : IAccessControlledProductService
{
    private bool IsAdmin() => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private static Exception AdminRoleRequired => new InvalidOperationException("Admin role is required to perform this operation");

    public Task<EntityResult<ProductReadDto>> AddAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? productService.AddAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> AddAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? productService.AddAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<ProductReadDto>> UpdateAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? productService.UpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> UpdateAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? productService.UpdateAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<ProductReadDto>> AddOrUpdateAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? productService.AddOrUpdateAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => IsAdmin() ? productService.RemoveAsync(dto, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => IsAdmin() ? productService.RemoveAsync(dtos, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => IsAdmin() ? productService.RemoveAsync(id, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => IsAdmin() ? productService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw AdminRoleRequired;

    public Task<EntityResult<ProductReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => productService.GetAsync(id, cancellationToken);

    public Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.CountAsync(descriptor, cancellationToken);

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.GetListAsync(descriptor, cancellationToken);

    public Task<EntityResult<PartedResult<ProductReadDto>>> GetPartedAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.GetPartedAsync(descriptor, cancellationToken);
}
