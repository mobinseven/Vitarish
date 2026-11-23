using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        TenantBusinessDetails? details = profile?.Details;
        return new TenantBusinessProfileView(tenant.Name, tenant.Description, tenant.LogoUrl, profile?.Category,
            details is { PublicListingAbsent: false } ? details : null,
            profile?.LogoPromptDismissedAt is not null, profile?.SetupGuideDismissedAt is not null,
            details?.TimeZoneId ?? TenantBusinessProfile.DefaultTimeZoneId);
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
        profile.ReplacePublicDetails(details);
        profile.UpdatedAt = nowUtc;
        await SaveProfileChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetTimeZoneAsync(Guid tenantId, string timeZoneId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        if (await FindTenantAsync(tenantId, tracked: false, cancellationToken) is null)
            return false;

        TenantBusinessProfile? profile = await dbContext.TenantBusinessProfiles
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (profile?.Details?.TimeZoneId == timeZoneId)
            return true;
        if (profile is null)
        {
            profile = new TenantBusinessProfile { TenantId = tenantId };
            dbContext.TenantBusinessProfiles.Add(profile);
        }

        profile.SetTimeZoneId(timeZoneId);
        profile.UpdatedAt = nowUtc;
        await SaveProfileChangesAsync(cancellationToken);
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

    public async Task<bool> DismissLogoPromptAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await FindTenantAsync(tenantId, tracked: false, cancellationToken);
        if (tenant is null)
            return false;
        TenantBusinessProfile? profile = await dbContext.TenantBusinessProfiles
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (profile is null)
        {
            profile = new TenantBusinessProfile { TenantId = tenantId, UpdatedAt = nowUtc };
            dbContext.TenantBusinessProfiles.Add(profile);
        }

        profile.LogoPromptDismissedAt ??= nowUtc;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DismissSetupGuideAsync(Guid tenantId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        Tenant? tenant = await FindTenantAsync(tenantId, tracked: false, cancellationToken);
        if (tenant is null)
            return false;
        TenantBusinessProfile? profile = await dbContext.TenantBusinessProfiles
            .SingleOrDefaultAsync(item => item.TenantId == tenantId, cancellationToken);
        if (profile is null)
        {
            profile = new TenantBusinessProfile { TenantId = tenantId, UpdatedAt = nowUtc };
            dbContext.TenantBusinessProfiles.Add(profile);
        }

        profile.SetupGuideDismissedAt ??= nowUtc;
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Task<Tenant?> FindTenantAsync(Guid tenantId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> tenants = tracked ? dbContext.Tenants : dbContext.Tenants.AsNoTracking();
        return tenants.SingleOrDefaultAsync(item => item.Id == tenantId && item.DeletedAt == null, cancellationToken);
    }

    private async Task SaveProfileChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new TenantBusinessProfileWriteConflictException(exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            && exception.Entries.Any(entry => entry.Entity is TenantBusinessProfile && entry.State == EntityState.Added))
        {
            throw new TenantBusinessProfileWriteConflictException(exception);
        }
    }
}
