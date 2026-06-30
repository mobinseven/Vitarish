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
    public const string TenantHostHeaderName = "X-Vitarish-Tenant-Host";

    private const string OriginHeaderName = "Origin";
    private const string RefererHeaderName = "Referer";
    private static readonly MemoryCacheEntryOptions CacheOptions = new() { SlidingExpiration = TimeSpan.FromMinutes(10) };

    /// <summary>
    /// Resolves an active tenant by normalizing the host name and looking up a matching indicator.
    /// </summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matched tenant, or null when no active indicator exists.</returns>
    public async Task<Tenant?> ResolveAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        string? host = ResolveTenantHost(context);
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

        string cacheKey = $"cachedTenant-resolver::{normalized}";
        bool cached = cache.TryGetValue(cacheKey, out object? cachedTenant);
        if (cached)
        {
            return cachedTenant as Tenant;
        }

        Tenant? tenant = await dbContext.TenantIndicators
                .Include(indicator => indicator.Tenant)
                .Where(indicator => indicator.Status == IndicatorStatus.Active)
                .Where(indicator => indicator.Value == normalized)
                .Select(indicator => indicator.Tenant)
                .FirstOrDefaultAsync(cancellationToken);

        _ = cache.Set(cacheKey, tenant, CacheOptions);

        return tenant;
    }

    private static string? ResolveTenantHost(HttpContext context)
    {
        string? tenantHost = ReadHostHeader(context, TenantHostHeaderName);
        string? originHost = ReadUriHostHeader(context, OriginHeaderName);
        string? refererHost = ReadUriHostHeader(context, RefererHeaderName);

        if (!string.IsNullOrWhiteSpace(tenantHost))
        {
            if (originHost is null && refererHost is null)
            {
                return tenantHost;
            }

            if (HostsMatch(tenantHost, originHost) || HostsMatch(tenantHost, refererHost))
            {
                return tenantHost;
            }
        }

        return originHost ?? refererHost ?? context.Request.Host.Host;
    }

    private static string? ReadHostHeader(HttpContext context, string headerName)
    {
        string? value = ReadFirstHeaderValue(context, headerName);
        if (value is null)
        {
            return null;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        return value;
    }

    private static string? ReadUriHostHeader(HttpContext context, string headerName)
    {
        string? value = ReadFirstHeaderValue(context, headerName);
        if (value is null || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.Host
            : null;
    }

    private static string? ReadFirstHeaderValue(HttpContext context, string headerName)
    {
        string value = context.Request.Headers[headerName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string firstValue = value.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        return string.IsNullOrWhiteSpace(firstValue) ? null : firstValue;
    }

    private static bool HostsMatch(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
        {
            return false;
        }

        try
        {
            return string.Equals(
                HostnameNormalizer.Canonical(left),
                HostnameNormalizer.Canonical(right),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
