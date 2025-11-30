using System.Security.Claims;
using Vitarish.Identity.Core;
using Vitarish.Identity.Core.Abstractions;

namespace Vitarish.Identity.Infrastructure.Context;

/// <summary>
/// Claims-based implementation of <see cref="IAdminContext"/>.
/// Provides administrative context information from a <see cref="ClaimsPrincipal"/>.
/// </summary>
public class ClaimsAdminContext : IAdminContext
{
    private readonly ClaimsPrincipal _principal;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaimsAdminContext"/>.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    public ClaimsAdminContext(ClaimsPrincipal principal)
    {
        _principal = principal ?? throw new ArgumentNullException(nameof(principal));
    }

    /// <inheritdoc />
    public string? UserId => _principal.FindFirst(IdentityConstants.ClaimTypes.Subject)?.Value;

    /// <inheritdoc />
    public string? OwnerId => _principal.FindFirst(IdentityConstants.ClaimTypes.OwnerId)?.Value;

    /// <inheritdoc />
    public bool IsAdmin => _principal.IsInRole(IdentityConstants.Admin);
}
