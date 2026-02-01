using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockPartiesService : IPartiesService
{
    private readonly List<Owner> owners = [];
    private readonly List<Tenant> tenants = [];

    public Task<Owner?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(owners.FirstOrDefault(owner => owner.Id == id));
    }

    public Task<IEnumerable<Owner>> GetOwnersAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<Owner>>(owners);
    }

    public Task<Owner?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        Owner owner = new()
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Name = dto.Name,
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };
        owners.Add(owner);
        return Task.FromResult<Owner?>(owner);
    }

    public Task<Owner?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        Owner? owner = owners.FirstOrDefault(item => item.Id == dto.Id);
        if (owner is null)
        {
            return Task.FromResult<Owner?>(null);
        }

        owner.Name = dto.Name;
        owner.LogoUrl = dto.LogoUrl;
        owner.Description = dto.Description;
        owner.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Owner?>(owner);
    }

    public Task DeleteOwnerAsync(Guid id, CancellationToken ct = default)
    {
        _ = owners.RemoveAll(owner => owner.Id == id);
        return Task.CompletedTask;
    }

    public Task<Tenant?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(tenants.FirstOrDefault(tenant => tenant.Id == id));
    }

    public Task<IEnumerable<Tenant>> GetTenantsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<Tenant>>(tenants);
    }

    public Task<IEnumerable<Tenant>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<Tenant>>(tenants.Where(tenant => tenant.OwnerId == ownerId).ToList());
    }

    public Task<Tenant?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        Tenant tenant = new()
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            OwnerId = dto.OwnerId,
            Name = dto.Name,
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            ClientId = dto.ClientId,
            Metadata = dto.Metadata,
            CreatedAt = DateTimeOffset.UtcNow
        };
        tenants.Add(tenant);
        return Task.FromResult<Tenant?>(tenant);
    }

    public Task<Tenant?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        Tenant? tenant = tenants.FirstOrDefault(item => item.Id == dto.Id);
        if (tenant is null)
        {
            return Task.FromResult<Tenant?>(null);
        }

        tenant.OwnerId = dto.OwnerId;
        tenant.Name = dto.Name;
        tenant.LogoUrl = dto.LogoUrl;
        tenant.Description = dto.Description;
        tenant.ClientId = dto.ClientId;
        tenant.Metadata = dto.Metadata;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        return Task.FromResult<Tenant?>(tenant);
    }

    public Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        _ = tenants.RemoveAll(tenant => tenant.Id == id);
        return Task.CompletedTask;
    }
}
