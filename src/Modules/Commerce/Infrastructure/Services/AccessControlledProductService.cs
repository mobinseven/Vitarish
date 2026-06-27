using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Commerce.Infrastructure.Services;

internal class AccessControlledProductService(
    IRequestContext requestContext,
    IProductService productService) : IAccessControlledProductService
{
    private bool ElevatedAccess() => requestContext.IsPlatformAdmin() || requestContext.IsTenantOwner();

    private static Exception ElevatedAccessRequired => new InvalidOperationException("Elevated access is required to perform this operation");

    public Task<EntityResult<ProductReadDto>> AddAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.AddAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> AddAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.AddAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<ProductReadDto>> UpdateAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.UpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> UpdateAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.UpdateAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<ProductReadDto>> AddOrUpdateAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.AddOrUpdateAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(ProductWriteDto dto, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.RemoveAsync(dto, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(IReadOnlyList<ProductWriteDto> dtos, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.RemoveAsync(dtos, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.RemoveAsync(id, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult> RemoveByDescriptorAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => ElevatedAccess() ? productService.RemoveByDescriptorAsync(descriptor, cancellationToken) : throw ElevatedAccessRequired;

    public Task<EntityResult<ProductReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
        => productService.GetAsync(id, cancellationToken);

    public Task<EntityResult<int>> CountAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.CountAsync(descriptor, cancellationToken);

    public Task<EntityResult<IReadOnlyList<ProductReadDto>>> GetListAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.GetListAsync(descriptor, cancellationToken);

    public Task<EntityResult<PartedResult<ProductReadDto>>> GetPartedAsync(ProductDescriptorDto descriptor, CancellationToken cancellationToken)
        => productService.GetPartedAsync(descriptor, cancellationToken);
}
