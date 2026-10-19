namespace Vitarish.Parties.Application.Abstractions;

/// <summary>
/// Whether a browser origin is an active tenant's own address under the tenant base domain.
/// Exact, configured origins are the host's concern; this answers only for tenant addresses.
/// </summary>
public interface ITenantOriginTrust
{
    Task<bool> IsTenantOriginAsync(Uri origin, CancellationToken cancellationToken = default);
}
