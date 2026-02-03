using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Resolves tenants based on incoming host names using cached tenant indicators.
/// </summary>
public class TenantResolver(PartiesDbContext dbContext, IMemoryCache cache) : ITenantResolver
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new() { SlidingExpiration = TimeSpan.FromMinutes(10) };

    /// <summary>
    /// Resolves an active tenant by normalizing the host name and looking up a matching indicator.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched tenant, or null when no active indicator exists.</returns>
    public async Task<Tenant?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        string host = context.Request.Host.Host;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }

        string normalized;
        try
        {
            normalized = HostnameNormalizer.Canonical(host);
        }
        catch (ArgumentException)
        {
            return null;
        }

        string cacheKey = $"tenant-resolver::{normalized}";
        return await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            _ = entry.SetOptions(CacheOptions);

            return await dbContext.TenantIndicators
                .Include(indicator => indicator.Tenant)
                .Where(indicator => indicator.Status == IndicatorStatus.Active)
                .Where(indicator => indicator.Value == normalized)
                .Select(indicator => indicator.Tenant)
                .FirstOrDefaultAsync(cancellationToken);
        });
    }
}
