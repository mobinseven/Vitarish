using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Main.Mock.Services;

public sealed class MockPartiesService : IPartiesService
{
    private readonly Dictionary<Guid, OwnerReadDto> owners = [];
    private readonly Dictionary<Guid, TenantReadDto> tenants = [];

    public Task<OwnerReadDto?> GetOwnerAsync(Guid id, CancellationToken ct = default)
    {
        _ = owners.TryGetValue(id, out OwnerReadDto? owner);
        return Task.FromResult(owner);
    }

    public Task<IEnumerable<OwnerReadDto>> GetOwnersAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<OwnerReadDto>>(owners.Values);
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
        owners[owner.Id] = owner;
        return Task.FromResult<OwnerReadDto?>(owner);
    }

    public Task<OwnerReadDto?> UpdateOwnerAsync(OwnerWriteDto dto, CancellationToken ct = default)
    {
        if (!owners.TryGetValue(dto.Id, out OwnerReadDto? owner))
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
        owners[dto.Id] = updatedOwner;
        return Task.FromResult<OwnerReadDto?>(updatedOwner);
    }

    public Task DeleteOwnerAsync(Guid id, CancellationToken ct = default)
    {
        _ = owners.Remove(id);
        return Task.CompletedTask;
    }

    public Task<TenantReadDto?> GetTenantAsync(Guid id, CancellationToken ct = default)
    {
        _ = tenants.TryGetValue(id, out TenantReadDto? tenant);
        return Task.FromResult(tenant);
    }

    public Task<IEnumerable<TenantReadDto>> GetTenantsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TenantReadDto>>(tenants.Values);
    }

    public Task<IEnumerable<TenantReadDto>> GetTenantsByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TenantReadDto>>(tenants.Values.Where(tenant => tenant.OwnerId == ownerId).ToList());
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
        tenants[tenant.Id] = tenant;
        return Task.FromResult<TenantReadDto?>(tenant);
    }

    public Task<TenantReadDto?> UpdateTenantAsync(TenantWriteDto dto, CancellationToken ct = default)
    {
        if (!tenants.TryGetValue(dto.Id, out TenantReadDto? tenant))
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
        tenants[dto.Id] = updatedTenant;
        return Task.FromResult<TenantReadDto?>(updatedTenant);
    }

    public Task DeleteTenantAsync(Guid id, CancellationToken ct = default)
    {
        _ = tenants.Remove(id);
        return Task.CompletedTask;
    }
}
