using MediatR;
using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Users.Application.Commands;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService(OwnersService ownerService, TenantsService tenantService, TenantIndicatorService tenantIndicatorService, ISender sender) : IPartiesService
{
    // Owner methods using EntityService
    public async Task<OwnerReadDto?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await ownerService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<OwnerReadDto>> GetOwnersAsync(CancellationToken ct = default)
    {
        EntityResult<IReadOnlyList<OwnerReadDto>> result = await ownerService.GetListAsync(new OwnerDescriptor(), ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<OwnerReadDto?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await ownerService.AddAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<OwnerReadDto?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await ownerService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteOwnerAsync(Guid id, CancellationToken ct = default) => await ownerService.RemoveAsync(id, ct);

    // Tenant methods using EntityService
    public async Task<TenantReadDto?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await tenantService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<TenantReadDto>> GetTenantsAsync(CancellationToken ct = default)
    {
        EntityResult<IReadOnlyList<TenantReadDto>> result = await tenantService.GetListAsync(new TenantDescriptor(), ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<IEnumerable<TenantReadDto>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        TenantDescriptor descriptor = new() { OwnerId = ownerId };
        EntityResult<IReadOnlyList<TenantReadDto>> result = await tenantService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<TenantReadDto?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await tenantService.AddAsync(dto, ct);
        if (result.IsSuccess)
        {
            await sender.Send(new CreateTenantRolesCommand(result.Value.Id), ct);
        }
        return result.IsSuccess ? result.Value : null;
    }

    public Task CreateTenantRolesAsync(Guid tenantId, CancellationToken ct = default) => sender.Send(new CreateTenantRolesCommand(tenantId), ct);

    public async Task<TenantReadDto?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await tenantService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default) => await tenantService.RemoveAsync(id, ct);

    // TenantIndicator methods using EntityService
    public async Task<TenantIndicatorReadDto?> GetTenantIndicatorAsync(Guid id, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await tenantIndicatorService.GetAsync(id, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<IEnumerable<TenantIndicatorReadDto>> GetTenantIndicatorsByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        TenantIndicatorDescriptor descriptor = new() { TenantId = tenantId };
        EntityResult<IReadOnlyList<TenantIndicatorReadDto>> result = await tenantIndicatorService.GetListAsync(descriptor, ct);
        return result.IsSuccess ? result.Value ?? [] : [];
    }

    public async Task<TenantIndicatorReadDto?> CreateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await tenantIndicatorService.AddAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task<TenantIndicatorReadDto?> UpdateTenantIndicatorAsync(TenantIndicatorWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantIndicatorReadDto> result = await tenantIndicatorService.UpdateAsync(dto, ct);
        return result.IsSuccess ? result.Value : null;
    }

    public async Task DeleteTenantIndicatorAsync(Guid id, CancellationToken ct = default) => await tenantIndicatorService.RemoveAsync(id, ct);
}
