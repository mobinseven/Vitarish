namespace Vitarish.Parties.Application;

/// <summary>Reads which platform app owns a tenant.</summary>
public interface IAppTenantDirectory
{
    /// <summary>True when <paramref name="tenantId"/> is a live tenant of the app <paramref name="appId"/>.</summary>
    Task<bool> IsLiveAppTenantAsync(Guid tenantId, string appId, CancellationToken cancellationToken = default);
}
