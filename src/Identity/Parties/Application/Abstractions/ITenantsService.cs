using Microsoft.AspNetCore.Http;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Parties.Application.Abstractions;

/// <summary>
/// Service for managing tenant information and resolution on the server side.
/// </summary>
public interface ITenantsService
{
    /// <summary>
    /// Resolves a tenant from the current HTTP request context using the host.
    /// </summary>
    /// <param name="httpContext">The HTTP context to resolve tenant from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved tenant or null if not found.</returns>
    Task<Tenant?> ResolveFromHostAsync(HttpContext httpContext, CancellationToken ct = default);
}
