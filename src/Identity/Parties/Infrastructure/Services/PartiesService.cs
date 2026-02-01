
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService : IPartiesService
{
    private readonly OwnerService _ownerService;
    private readonly TenantService _tenantService;
    private readonly PartiesDbContext _dbContext;

    public PartiesService(OwnerService ownerService, TenantService tenantService, PartiesDbContext dbContext)
    {
        _ownerService = ownerService;
        _tenantService = tenantService;
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
}
