using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Contracts;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Dtos;
using Vitarish.Parties.Domain.Models;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class PartiesService(
    OwnersService ownerService, 
    TenantsService tenantService, 
    TenantIndicatorService tenantIndicatorService, 
    ISender sender,
    PartiesDbContext dbContext) : IPartiesService
{
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

        // 4. Assign the onboarding user to all three tenant roles so they have full access.
        User? user = await sender.Send(new GetUserQuery(request.UserId), ct);
        if (user is not null)
        {
            foreach (string roleSuffix in new[] { IdentityDefinitions.TenantOwner, IdentityDefinitions.TenantUser })
            {
                // Roles were already created by CreateTenantRolesCommand above.
                // The AddUserToRoleCommand handler uses the role name as-is when no tenant
                // is active in the request context, so we pass the fully-qualified name.
                _ = await sender.Send(new AddUserToRoleCommand(user, $"{tenant.Id}:{roleSuffix}"), ct);
            }
        }

        return tenant;
    }
}
