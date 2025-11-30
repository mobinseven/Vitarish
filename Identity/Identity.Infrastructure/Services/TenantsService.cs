using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vitarish.Identity.Core.Application.Abstractions;
using Vitarish.Identity.Core.Domain.Entities;
using Vitarish.Identity.Core.Domain.Enums;
using Vitarish.Identity.Core.Extensions;
using Vitarish.Identity.Infrastructure.Data;

namespace Vitarish.Identity.Infrastructure.Services;

/// <summary>
/// Service for tenant resolution from hostname.
/// </summary>
internal sealed class TenantsService(
    IdentityDbContext db,
    IMemoryCache cache,
    ILogger<TenantsService> logger) : ITenantsService
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<Tenant?> ResolveFromHostAsync(string hostname, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            logger.LogWarning("Tenant resolution failed: Host is empty");
            return null;
        }

        string canonical;
        try
        {
            canonical = HostnameNormalizer.Canonical(hostname);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Tenant resolution failed: Invalid hostname format {Host}", hostname);
            return null;
        }

        string cacheKey = $"tenant:host:{canonical}";
        if (cache.TryGetValue(cacheKey, out Tenant? cachedTenant))
        {
            return cachedTenant;
        }

        // Resolve by hostname first, then subdomain (both must be Active)
        List<Tenant> tenants = await db.Tenants
            .AsNoTracking()
            .Include(t => t.Indicators)
            .Where(t => t.Indicators.Any(i =>
                i.Status == IndicatorStatus.Active &&
                (i.Method == IndicatorMethod.Hostname || i.Method == IndicatorMethod.Subdomain) &&
                i.Value == canonical))
            .ToListAsync(ct);

        if (tenants.Count == 0)
        {
            logger.LogInformation("No tenant found for {Host} (canonical: {Canonical})", hostname, canonical);
            return null;
        }

        if (tenants.Count > 1)
        {
            // Multiple tenants match - this shouldn't happen with unique constraints
            logger.LogError("Multiple tenants found for {Host} (canonical: {Canonical})", hostname, canonical);
            throw new InvalidOperationException($"Ambiguous tenant mapping for host: {canonical}");
        }

        Tenant resolved = tenants[0];
        logger.LogInformation(
            "Tenant resolved for {Host} (canonical: {Canonical}): TenantId={TenantId}, Name={Name}",
            hostname, canonical, resolved.Id, resolved.Name);

        cache.Set(cacheKey, resolved, CacheExpiration);
        return resolved;
    }
}
