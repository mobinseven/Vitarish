using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService(
    OwnersService ownerService, 
    TenantsService tenantService, 
    TenantIndicatorService tenantIndicatorService, 
    ISender sender,
    IUsersService usersService,
    PartiesDbContext dbContext,
    IMemoryCache cache) : IPartiesService
{
    public async Task<LandingHostBinding> GetLandingHostBindingAsync(string hostname, CancellationToken ct = default)
    {
        string normalized = HostnameNormalizer.Canonical(hostname);
        Guid? tenantId = await dbContext.TenantIndicators.AsNoTracking()
            .Where(indicator => indicator.Method == Vitarish.Parties.Domain.Enums.IndicatorMethod.Hostname)
            .Where(indicator => indicator.Value == normalized && indicator.Status == Vitarish.Parties.Domain.Enums.IndicatorStatus.Active)
            .Select(indicator => (Guid?)indicator.TenantId)
            .FirstOrDefaultAsync(ct);
        return new LandingHostBinding(normalized, tenantId);
    }

    public async Task<LandingHostBindingChange?> SetLandingHostBindingAsync(string hostname, Guid tenantId, CancellationToken ct = default)
    {
        string normalized = HostnameNormalizer.Canonical(hostname);
        if (!await dbContext.Tenants.AnyAsync(item => item.Id == tenantId, ct)) return null;
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        TenantIndicator? indicator = await dbContext.TenantIndicators.SingleOrDefaultAsync(item => item.Method == Domain.Enums.IndicatorMethod.Hostname && item.Value == normalized, ct);
        Guid? previous = indicator?.TenantId;
        if (indicator is null)
        {
            bool targetHasPrimary = await dbContext.TenantIndicators.AnyAsync(item => item.TenantId == tenantId && item.IsPrimary, ct);
            indicator = new TenantIndicator { Id = Guid.NewGuid(), TenantId = tenantId, Method = Domain.Enums.IndicatorMethod.Hostname, Value = normalized, Status = Domain.Enums.IndicatorStatus.Active, IsPrimary = !targetHasPrimary };
            dbContext.TenantIndicators.Add(indicator);
        }
        else if (indicator.TenantId != tenantId)
        {
            bool targetHasPrimary = await dbContext.TenantIndicators.AnyAsync(item => item.TenantId == tenantId && item.IsPrimary && item.Id != indicator.Id, ct);
            indicator.TenantId = tenantId; indicator.Status = Domain.Enums.IndicatorStatus.Active; indicator.IsPrimary = !targetHasPrimary;
        }
        else indicator.Status = Domain.Enums.IndicatorStatus.Active;
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        cache.Remove($"cachedTenant-resolver::{normalized}");
        return new LandingHostBindingChange(normalized, previous, tenantId, previous != tenantId);
    }
    public async Task<IEnumerable<UserTenant>> GetUserTenantsAsync(CancellationToken ct = default)
    {
        // Query the cross-schema view that retrieves tenants for the current user
        // The view uses current_setting('app.user_id', true) set via RequestContextInterceptor
        return await dbContext.UserTenants.ToListAsync(ct);
    }
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

    public async Task<TenantReadDto?> SetupTenantAsync(SetupTenantRequest request, CancellationToken ct = default)
    {
        // 1. Create the owner entity (CreatedBy is set automatically by the entity interceptor,
        //    establishing the implicit Owner↔User mapping).
        OwnerReadDto? owner = await CreateOwnerAsync(
            new OwnerWriteDto { Id = Guid.NewGuid(), Name = request.OwnerName }, ct);
        if (owner is null)
        {
            return null;
        }

        // 2. Create the tenant, persisting the selected module list in metadata.
        TenantWriteDto tenantDto = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = owner.Id,
            Name = request.TenantName,
            AppId = request.AppId,
            Metadata = new Dictionary<string, object?> { [TenantMetadata.ActiveModulesKey] = request.SelectedModules }
        };
        // CreateTenantAsync also issues CreateTenantRolesCommand internally.
        TenantReadDto? tenant = await CreateTenantAsync(tenantDto, ct);
        if (tenant is null)
        {
            return null;
        }

        // 3. Create the primary indicator so the tenant can be resolved by hostname/subdomain.
        _ = await CreateTenantIndicatorAsync(new TenantIndicatorWriteDto
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            Method = request.IndicatorMethod,
            Value = request.IndicatorValue,
            Status = request.IndicatorStatus,
            IsPrimary = true
        }, ct);

        // 4. Assign the onboarding user the single effective tenant role.
        User? user = await sender.Send(new GetUserQuery(request.UserId), ct);
        if (user is not null)
        {
            // Roles were already created by CreateTenantRolesCommand above. TenantOwner is
            // the single effective tenant role for the onboarding account.
            IdentityResult result = await usersService.ReplaceTenantRoleAsync(user.Id, tenant.Id, IdentityDefinitions.TenantOwner, ct);
            if (!result.Succeeded)
            {
                return null;
            }
        }

        return tenant;
    }
}
