using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
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

    public Task<Owner?> CreateOwnerAsync(CreateOwner request, CancellationToken ct = default)
    {
        Owner owner = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Description = request.Description
        };
        owners.Add(owner);
        return Task.FromResult<Owner?>(owner);
    }

    public Task<Owner?> UpdateOwnerAsync(UpdateOwner request, CancellationToken ct = default)
    {
        Owner? owner = owners.FirstOrDefault(item => item.Id == request.Id);
        if (owner is null)
        {
            return Task.FromResult<Owner?>(null);
        }

        owner.Name = request.Name;
        owner.LogoUrl = request.LogoUrl;
        owner.Description = request.Description;
        owner.ModifiedAt = DateTime.UtcNow;
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

    public Task<Tenant?> CreateTenantAsync(CreateTenant request, CancellationToken ct = default)
    {
        Tenant tenant = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            Name = request.Name,
            LogoUrl = request.LogoUrl,
            Description = request.Description,
            ClientId = request.ClientId
        };
        tenants.Add(tenant);
        return Task.FromResult<Tenant?>(tenant);
    }

    public Task<Tenant?> UpdateTenantAsync(UpdateTenant request, CancellationToken ct = default)
    {
        Tenant? tenant = tenants.FirstOrDefault(item => item.Id == request.Id);
        if (tenant is null)
        {
            return Task.FromResult<Tenant?>(null);
        }

        tenant.Name = request.Name;
        tenant.LogoUrl = request.LogoUrl;
        tenant.Description = request.Description;
        tenant.ClientId = request.ClientId;
        tenant.ModifiedAt = DateTime.UtcNow;
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
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, static indicator => indicator.Status = IndicatorStatus.Verified));
    }

    public Task<bool> ActivateTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, static indicator => indicator.Status = IndicatorStatus.Active));
    }

    public Task<bool> DisableTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        return Task.FromResult(UpdateIndicatorStatus(tenantId, indicatorId, static indicator => indicator.Status = IndicatorStatus.Disabled));
    }

    public Task<bool> DeleteTenantIndicatorAsync(Guid tenantId, Guid indicatorId, CancellationToken ct = default)
    {
        int removed = indicators.RemoveAll(indicator => indicator.TenantId == tenantId && indicator.Id == indicatorId);
        return Task.FromResult(removed > 0);
    }

    private bool UpdateIndicatorStatus(Guid tenantId, Guid indicatorId, Action<TenantIndicator> update)
    {
        TenantIndicator? indicator = indicators.FirstOrDefault(item => item.TenantId == tenantId && item.Id == indicatorId);
        if (indicator is null)
        {
            return false;
        }

        update(indicator);
        indicator.VerifiedAt = DateTimeOffset.UtcNow;
        return true;
    }
}
