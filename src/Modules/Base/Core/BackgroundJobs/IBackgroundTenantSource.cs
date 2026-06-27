namespace Vitarish.Modules.Base.BackgroundJobs;

/// <summary>
/// Provides tenant identities for tenant-scoped background work.
/// </summary>
public interface IBackgroundTenantSource
{
    /// <summary>
    /// Returns the tenants that should receive background job execution.
    /// </summary>
    Task<IEnumerable<BackgroundTenant>> GetTenantsAsync(CancellationToken cancellationToken = default);
}
