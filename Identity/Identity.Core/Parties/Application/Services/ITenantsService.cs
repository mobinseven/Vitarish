using Vitarish.Identity.Core.Parties.Domain.Entities;

namespace Vitarish.Identity.Core.Parties.Application.Services;

/// <summary>
/// Service for managing tenant information and resolution on the server side.
/// </summary>
public interface ITenantsService
{
    /// <summary>
    /// Resolves a tenant from a host value.
    /// </summary>
    /// <param name="host">The host name to resolve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved tenant or null if not found.</returns>
    Task<Tenant?> ResolveFromHostAsync(string host, CancellationToken ct = default);
}
