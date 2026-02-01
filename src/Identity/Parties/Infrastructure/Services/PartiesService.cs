
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Application.Descriptors;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService : IPartiesService
{
    private readonly OwnerService _ownerService;
    private readonly TenantService _tenantService;
    private readonly ISender _sender;
    private readonly PartiesDbContext _dbContext;

    public PartiesService(OwnerService ownerService, TenantService tenantService, ISender sender, PartiesDbContext dbContext)
    {
        _ownerService = ownerService;
        _tenantService = tenantService;
        _sender = sender;
        _dbContext = dbContext;
    }

    // Owner methods using EntityService
    public async Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Owners.FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default)
    {
        return await _dbContext.Owners.Where(o => o.DeletedAt == null).ToListAsync(ct);
    }

    public async Task<Owner?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await _ownerService.AddAsync(dto, ct);
        if (!result.IsSuccess || result.Value == null)
            return null;

        return await GetOwnerAsync(result.Value.Id, ct);
    }

    public async Task<Owner?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<OwnerReadDto> result = await _ownerService.UpdateAsync(dto, ct);
        if (!result.IsSuccess || result.Value == null)
            return null;

        return await GetOwnerAsync(result.Value.Id, ct);
    }

    public async Task DeleteOwnerAsync(Guid id, CancellationToken ct = default)
    {
        await _ownerService.RemoveAsync(id, ct);
    }

    // Tenant methods using EntityService
    public async Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .Include(t => t.Indicators)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .Include(t => t.Indicators)
            .Where(t => t.DeletedAt == null)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return await _dbContext.Tenants
            .Include(t => t.Indicators)
            .Where(t => t.OwnerId == ownerId && t.DeletedAt == null)
            .ToListAsync(ct);
    }

    public async Task<Tenant?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await _tenantService.AddAsync(dto, ct);
        if (!result.IsSuccess || result.Value == null)
            return null;

        return await GetTenantAsync(result.Value.Id, ct);
    }

    public async Task<Tenant?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        EntityResult<TenantReadDto> result = await _tenantService.UpdateAsync(dto, ct);
        if (!result.IsSuccess || result.Value == null)
            return null;

        return await GetTenantAsync(result.Value.Id, ct);
    }

    public async Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        await _tenantService.RemoveAsync(id, ct);
    }

    // TenantIndicator methods - keep using MediatR for now
    public Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default) =>
        _sender.Send(new GetTenantIndicators(tenantId), ct);

    public async Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicator request, CancellationToken ct = default) =>
        await _sender.Send(request, ct);

    public Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        _sender.Send(new VerifyTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        _sender.Send(new ActivateTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        _sender.Send(new DisableTenantIndicator(tenantId, indicatorId), ct);

    public Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default) =>
        _sender.Send(new DeleteTenantIndicator(tenantId, indicatorId), ct);
}
