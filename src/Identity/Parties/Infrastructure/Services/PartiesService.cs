using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Descriptors;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService : IPartiesService
{
    private readonly OwnerService _ownerService;
    private readonly TenantService _tenantService;

    public PartiesService(OwnerService ownerService, TenantService tenantService)
    {
        _ownerService = ownerService;
        _tenantService = tenantService;
    }

    // Owner methods using EntityService
    public async Task<OwnerReadDto?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await _ownerService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<OwnerReadDto>> GetOwnersAsync(CancellationToken ct = default)
    {
        OwnerDescriptor descriptor = new() { DeletedFrom = null, DeletedTo = null };
        EntityResult<IReadOnlyList<OwnerReadDto>> result = await _ownerService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<OwnerReadDto?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await _ownerService.AddAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<OwnerReadDto?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await _ownerService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteOwnerAsync(Guid id, CancellationToken ct = default) => await _ownerService.RemoveAsync(id, ct);

    // Tenant methods using EntityService
    public async Task<TenantReadDto?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await _tenantService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<TenantReadDto>> GetTenantsAsync(CancellationToken ct = default)
    {
        TenantDescriptor descriptor = new() { DeletedFrom = null, DeletedTo = null };
        EntityResult<IReadOnlyList<TenantReadDto>> result = await _tenantService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<IEnumerable<TenantReadDto>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        TenantDescriptor descriptor = new() { OwnerId = ownerId, DeletedFrom = null, DeletedTo = null };
        EntityResult<IReadOnlyList<TenantReadDto>> result = await _tenantService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<TenantReadDto?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await _tenantService.AddAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<TenantReadDto?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await _tenantService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default) => await _tenantService.RemoveAsync(id, ct);
}
