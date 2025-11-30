using Vitarish.Identity.Core.Domain.Entities;

namespace Vitarish.Identity.Core.Application.Abstractions;

/// <summary>
/// Service for managing tenant information and resolution on the server side.
/// </summary>
public interface ITenantsService
{
    /// <summary>
    /// Resolves a tenant from the current hostname.
    /// </summary>
    /// <param name="hostname">The hostname to resolve tenant from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved tenant or null if not found.</returns>
    Task<Tenant?> ResolveFromHostAsync(string hostname, CancellationToken ct = default);
}
