using System.Security.Claims;

namespace Vitarish.Identity.Contracts.Claims;

/// <summary>
/// Extension methods for working with identity claims.
/// These extensions replace the legacy RequestIdentities approach with standard claims-based access.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the user's unique identifier from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID or null if not found.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(IdentityClaimTypes.UserId)
            ?? principal.FindFirstValue(IdentityClaimTypes.Subject)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Gets the owner's unique identifier from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The owner ID or null if not found.</returns>
    public static string? GetOwnerId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(IdentityClaimTypes.OwnerId);
    }

    /// <summary>
    /// Gets the tenant's unique identifier from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The tenant ID or null if not found.</returns>
    public static string? GetTenantId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(IdentityClaimTypes.TenantId);
    }

    /// <summary>
    /// Gets the authentication client's identifier from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The client ID or null if not found.</returns>
    public static string? GetClientId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(IdentityClaimTypes.ClientId);
    }

    /// <summary>
    /// Gets the user's access level from claims.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The access level or null if not found.</returns>
    public static string? GetAccessLevel(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(IdentityClaimTypes.AccessLevel);
    }

    /// <summary>
    /// Checks if the user has a specific claim.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type to check.</param>
    /// <param name="value">The expected value.</param>
    /// <returns>True if the claim exists with the specified value.</returns>
    public static bool HasClaim(this ClaimsPrincipal principal, string claimType, string value)
    {
        return principal.HasClaim(c => c.Type == claimType && c.Value == value);
    }

    /// <summary>
    /// Gets the first value of a claim by type.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="claimType">The claim type.</param>
    /// <returns>The claim value or null if not found.</returns>
    public static string? FindFirstValue(this ClaimsPrincipal principal, string claimType)
    {
        return principal.FindFirst(claimType)?.Value;
    }
}
