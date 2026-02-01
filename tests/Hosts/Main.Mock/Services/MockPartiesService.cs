using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Main.Mock.Services;

public sealed class MockPartiesService : IPartiesService
{
    private readonly List<OwnerReadDto> owners = [];
    private readonly List<TenantReadDto> tenants = [];

    public Task<OwnerReadDto?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(owners.FirstOrDefault(owner => owner.Id == id));
    }

    public Task<IEnumerable<OwnerReadDto>> GetOwnersAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<OwnerReadDto>>(owners);
    }

    public Task<OwnerReadDto?> CreateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        OwnerReadDto owner = new()
        {
            Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id,
            Name = dto.Name,
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };
        owners.Add(owner);
        return Task.FromResult<OwnerReadDto?>(owner);
    }

    public Task<OwnerReadDto?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        OwnerReadDto? owner = owners.FirstOrDefault(item => item.Id == dto.Id);
        if (owner is null)
        {
            return Task.FromResult<OwnerReadDto?>(null);
        }

        OwnerReadDto updatedOwner = owner with
        {
            Name = dto.Name,
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _ = owners.Remove(owner);
        owners.Add(updatedOwner);
        return Task.FromResult<OwnerReadDto?>(updatedOwner);
    }

    public Task DeleteOwnerAsync(Guid id, CancellationToken ct = default)
    {
        _ = owners.RemoveAll(owner => owner.Id == id);
        return Task.CompletedTask;
    }

    public Task<TenantReadDto?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(tenants.FirstOrDefault(tenant => tenant.Id == id));
    }

    public Task<IEnumerable<TenantReadDto>> GetTenantsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TenantReadDto>>(tenants);
    }

    public Task<IEnumerable<TenantReadDto>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TenantReadDto>>(tenants.Where(tenant => tenant.OwnerId == ownerId).ToList());
    }

    public Task<TenantReadDto?> CreateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        TenantReadDto tenant = new()
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
        return Task.FromResult<TenantReadDto?>(tenant);
    }

    public Task<TenantReadDto?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        TenantReadDto? tenant = tenants.FirstOrDefault(item => item.Id == dto.Id);
        if (tenant is null)
        {
            return Task.FromResult<TenantReadDto?>(null);
        }

        TenantReadDto updatedTenant = tenant with
        {
            OwnerId = dto.OwnerId,
            Name = dto.Name,
            LogoUrl = dto.LogoUrl,
            Description = dto.Description,
            ClientId = dto.ClientId,
            Metadata = dto.Metadata,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        _ = tenants.Remove(tenant);
        tenants.Add(updatedTenant);
        return Task.FromResult<TenantReadDto?>(updatedTenant);
    }

    public Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        _ = tenants.RemoveAll(tenant => tenant.Id == id);
        return Task.CompletedTask;
    }
}
