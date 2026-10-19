namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>Memory-cache keys of answers derived from tenant indicators, by canonical hostname.</summary>
public static class TenantHostCacheKeys
{
    public static string Resolver(string hostname) => $"cachedTenant-resolver::{hostname}";

    public static string OriginTrust(string hostname) => $"tenant-origin-trust::{hostname}";
}
