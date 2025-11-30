using System.Security.Claims;
using Vitarish.Identity.Core;
using Vitarish.Identity.Core.Abstractions;

namespace Vitarish.Identity.Infrastructure.Context;

/// <summary>
/// Claims-based implementation of <see cref="IIdentityContext"/>.
/// Extracts identity information from a <see cref="ClaimsPrincipal"/>.
/// </summary>
public class ClaimsIdentityContext : IIdentityContext
{
    private readonly ClaimsPrincipal _principal;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaimsIdentityContext"/>.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    public ClaimsIdentityContext(ClaimsPrincipal principal)
    {
        _principal = principal ?? throw new ArgumentNullException(nameof(principal));
    }

    /// <inheritdoc />
    public string? UserId => GetClaimValue(IdentityConstants.ClaimTypes.Subject);

    /// <inheritdoc />
    public string? OwnerId => GetClaimValue(IdentityConstants.ClaimTypes.OwnerId);

    /// <inheritdoc />
    public string? ClientId => GetClaimValue(IdentityConstants.ClaimTypes.ClientId);

    /// <inheritdoc />
    public string? TenantId => GetClaimValue(IdentityConstants.ClaimTypes.TenantId);

    /// <inheritdoc />
    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public string? DisplayName =>
        GetClaimValue(IdentityConstants.ClaimTypes.Name) ??
        $"{GetClaimValue(IdentityConstants.ClaimTypes.GivenName)} {GetClaimValue(IdentityConstants.ClaimTypes.FamilyName)}".Trim();

    /// <inheritdoc />
    public string? Email => GetClaimValue(IdentityConstants.ClaimTypes.Email);

    /// <inheritdoc />
    public bool HasClaim(string claimType, string claimValue)
    {
        return _principal.HasClaim(claimType, claimValue);
    }

    /// <inheritdoc />
    public string? GetClaimValue(string claimType)
    {
        return _principal.FindFirst(claimType)?.Value;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetClaimValues(string claimType)
    {
        return _principal.FindAll(claimType).Select(c => c.Value);
    }
}
