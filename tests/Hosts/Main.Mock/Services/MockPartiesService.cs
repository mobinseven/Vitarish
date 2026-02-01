using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Main.Mock.Services;

public sealed class MockPartiesService : IPartiesService
{
    private readonly List<Owner> owners = [];
    private readonly List<Tenant> tenants = [];
    private readonly List<TenantIndicator> indicators = [];

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
        _ = indicators.RemoveAll(indicator => indicator.TenantId == id);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TenantIndicator>> GetTenantIndicatorsAsync(Guid tenantId, CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<TenantIndicator>>(indicators.Where(indicator => indicator.TenantId == tenantId).ToList());
    }

    public Task<TenantIndicator?> AddTenantIndicatorAsync(AddTenantIndicator request, CancellationToken ct = default)
    {
        TenantIndicator indicator = new()
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Value = request.Value,
            Method = request.Method,
            Status = IndicatorStatus.PendingVerification,
            IsPrimary = request.IsPrimary,
            Verification = new VerificationInfo
            {
                Method = request.VerificationMethod,
                Evidence = "mock-evidence",
                EvidenceExpectedHash = "mock-hash",
                Token = "mock-token"
            }
        };
        indicators.Add(indicator);
        return Task.FromResult<TenantIndicator?>(indicator);
    }

    public Task<bool> VerifyTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, IndicatorStatus.Verified));
    }

    public Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, IndicatorStatus.Active));
    }

    public Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, IndicatorStatus.Disabled));
    }

    public Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        int removed = indicators.RemoveAll(indicator => indicator.TenantId == tenantId && indicator.Id == indicatorId);
        return Task.FromResult(removed > 0);
    }

    private bool UpdateIndicatorStatus(Guid tenantId, Guid indicatorId, IndicatorStatus newStatus)
    {
        TenantIndicator? indicator = indicators.FirstOrDefault(item => item.TenantId == tenantId && item.Id == indicatorId);
        if (indicator is null)
        {
            return false;
        }

        indicator.Status = newStatus;
        if (newStatus == IndicatorStatus.Verified)
        {
            indicator.VerifiedAt = DateTimeOffset.UtcNow;
        }
        return true;
    }
}
