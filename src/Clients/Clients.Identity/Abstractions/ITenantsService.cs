using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Identity.Abstractions;

/// <summary>
/// Service for managing tenant information and resolution.
/// </summary>
public interface ITenantsService
{
    /// <summary>
    /// Retrieves all tenants for a given client ID.
    /// </summary>
    /// <param name="clientId">The client ID to retrieve tenants for. If null, uses the current client ID from IIdentityClientContext.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A collection of tenants associated with the client.</returns>
    Task<IEnumerable<TenantInfo>> GetAllForClientAsync(string? clientId = null, CancellationToken ct = default);
}
