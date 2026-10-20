using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Profiles;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class TenantBusinessProfileStore(PartiesDbContext dbContext) : ITenantBusinessProfileStore
{
    public async Task<TenantBusinessProfileView?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await FindTenantAsync(tenantId, tracked: false, cancellationToken);
        if (tenant is null)
            return null;
        TenantBusinessProfile? profile = await dbContext.TenantBusinessProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        return new TenantBusinessProfileView(tenant.Name, tenant.Description, tenant.LogoUrl, profile?.Category, profile?.Details);
    }

    public async Task<bool> SaveAsync(Guid tenantId, string? description, string? category, TenantBusinessDetails? details, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await FindTenantAsync(tenantId, tracked: true, cancellationToken);
        if (tenant is null)
            return false;
        TenantBusinessProfile? profile = await dbContext.TenantBusinessProfiles
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (profile is null)
        {
            profile = new TenantBusinessProfile { TenantId = tenantId };
            dbContext.TenantBusinessProfiles.Add(profile);
        }

        tenant.Description = description;
        profile.Category = category;
        profile.Details = details;
        profile.UpdatedAt = nowUtc;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetLogoUrlAsync(Guid tenantId, string logoUrl, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await FindTenantAsync(tenantId, tracked: true, cancellationToken);
        if (tenant is null)
            return false;
        tenant.LogoUrl = logoUrl;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<Tenant?> FindTenantAsync(Guid tenantId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> tenants = tracked ? dbContext.Tenants : dbContext.Tenants.AsNoTracking();
        return tenants.SingleOrDefaultAsync(item => item.Id == tenantId && item.DeletedAt == null, cancellationToken);
    }
}
