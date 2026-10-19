namespace Vitarish.Parties.Application.Abstractions;

/// <summary>Hostnames already held by a tenant indicator.</summary>
public interface ITenantHostnameRegistry
{
    /// <summary>
    /// True when any tenant indicator holds <paramref name="hostname"/>, whatever its
    /// method or status, so a disabled or unverified address is never handed to someone else.
    /// </summary>
    Task<bool> IsClaimedAsync(string hostname, CancellationToken cancellationToken = default);
}
