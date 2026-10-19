using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Domain.Addresses;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Extensions;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Trusts <c>https://&lt;subdomain&gt;.&lt;base domain&gt;</c> (any port) while an active subdomain
/// indicator holds that hostname. Plain <c>http</c> is trusted only under a <c>localhost</c>
/// base domain, for local and E2E hosts. Answers are cached per instance and dropped when the
/// hostname's indicators change; the expiry bounds staleness on other instances.
/// </summary>
public sealed class TenantOriginTrust(
    PartiesDbContext dbContext,
    IMemoryCache cache,
    IOptions<TenantAddressOptions> options) : ITenantOriginTrust
{
    private const string LocalhostDomain = "localhost";
    private static readonly TimeSpan TrustedLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan UntrustedLifetime = TimeSpan.FromMinutes(1);

    public async Task<bool> IsTenantOriginAsync(Uri origin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(origin);
        TenantAddressOptions addresses = options.Value;
        if (!addresses.IsConfigured || !origin.IsAbsoluteUri)
            return false;

        string baseDomain;
        string hostname;
        try
        {
            baseDomain = HostnameNormalizer.Canonical(addresses.TenantBaseDomain!);
            hostname = HostnameNormalizer.Canonical(origin.Host);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!IsAllowedScheme(origin.Scheme, baseDomain) || !IsTenantSubdomainOf(hostname, baseDomain))
            return false;

        string cacheKey = TenantHostCacheKeys.OriginTrust(hostname);
        if (cache.TryGetValue(cacheKey, out bool cached))
            return cached;

        bool trusted = await dbContext.TenantIndicators.AsNoTracking().AnyAsync(
            indicator => indicator.Value == hostname
                && indicator.Method == IndicatorMethod.Subdomain
                && indicator.Status == IndicatorStatus.Active,
            cancellationToken);
        _ = cache.Set(cacheKey, trusted, trusted ? TrustedLifetime : UntrustedLifetime);
        return trusted;
    }

    private static bool IsAllowedScheme(string scheme, string baseDomain) =>
        scheme == Uri.UriSchemeHttps
        || (scheme == Uri.UriSchemeHttp
            && (baseDomain == LocalhostDomain || baseDomain.EndsWith($".{LocalhostDomain}", StringComparison.Ordinal)));

    // Exactly one well-formed, unreserved label above the base domain.
    private static bool IsTenantSubdomainOf(string hostname, string baseDomain)
    {
        string suffix = $".{baseDomain}";
        if (!hostname.EndsWith(suffix, StringComparison.Ordinal))
            return false;
        string label = hostname[..^suffix.Length];
        return TenantSubdomain.TryCreate(label, out TenantSubdomain? subdomain) == TenantSubdomainRejection.None
            && subdomain!.Value == label;
    }
}
