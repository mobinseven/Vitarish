using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Descriptors;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService : IPartiesService
{
    private readonly OwnerService _ownerService;
    private readonly TenantService _tenantService;
    private readonly TenantIndicatorService _tenantIndicatorService;

    public PartiesService(OwnerService ownerService, TenantService tenantService, TenantIndicatorService tenantIndicatorService)
    {
        _ownerService = ownerService;
        _tenantService = tenantService;
        _tenantIndicatorService = tenantIndicatorService;
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

    // TenantIndicator methods using EntityService
    public async Task<TenantIndicatorReadDto?> GetTenantIndicatorAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await _tenantIndicatorService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<TenantIndicatorReadDto>> GetTenantIndicatorsByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        TenantIndicatorDescriptor descriptor = new() { TenantId = tenantId, DeletedFrom = null, DeletedTo = null };
        EntityResult<IReadOnlyList<TenantIndicatorReadDto>> result = await _tenantIndicatorService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<TenantIndicatorReadDto?> CreateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await _tenantIndicatorService.AddAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<TenantIndicatorReadDto?> UpdateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await _tenantIndicatorService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteTenantIndicatorAsync(Guid id, CancellationToken ct = default) => await _tenantIndicatorService.RemoveAsync(id, ct);
}
