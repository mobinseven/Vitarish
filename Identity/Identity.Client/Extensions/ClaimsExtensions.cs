using System.Security.Claims;
using Vitarish.Identity.Contracts.Dtos;
using Vitarish.Identity.Core;

namespace Vitarish.Identity.Client.Extensions;

/// <summary>
/// Extension methods for working with claims.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Gets the user ID (subject claim) from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The user ID, or null if not found.</returns>
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirst(IdentityConstants.ClaimTypes.Subject)?.Value;

    /// <summary>
    /// Gets the user ID from a collection of claims.
    /// </summary>
    /// <param name="claims">The claims collection.</param>
    /// <returns>The user ID, or null if not found.</returns>
    public static string? GetUserId(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == IdentityConstants.ClaimTypes.Subject)?.Value;

    /// <summary>
    /// Gets the given name from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The given name, or null if not found.</returns>
    public static string? GetGivenName(this ClaimsPrincipal principal) =>
        principal.FindFirst(IdentityConstants.ClaimTypes.GivenName)?.Value;

    /// <summary>
    /// Gets the given name from a collection of claims.
    /// </summary>
    /// <param name="claims">The claims collection.</param>
    /// <returns>The given name, or null if not found.</returns>
    public static string? GetGivenName(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == IdentityConstants.ClaimTypes.GivenName)?.Value;

    /// <summary>
    /// Gets the family name from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The family name, or null if not found.</returns>
    public static string? GetFamilyName(this ClaimsPrincipal principal) =>
        principal.FindFirst(IdentityConstants.ClaimTypes.FamilyName)?.Value;

    /// <summary>
    /// Gets the family name from a collection of claims.
    /// </summary>
    /// <param name="claims">The claims collection.</param>
    /// <returns>The family name, or null if not found.</returns>
    public static string? GetFamilyName(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == IdentityConstants.ClaimTypes.FamilyName)?.Value;

    /// <summary>
    /// Gets the email from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The email, or null if not found.</returns>
    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(IdentityConstants.ClaimTypes.Email)?.Value;

    /// <summary>
    /// Gets the email from a collection of claims.
    /// </summary>
    /// <param name="claims">The claims collection.</param>
    /// <returns>The email, or null if not found.</returns>
    public static string? GetEmail(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == IdentityConstants.ClaimTypes.Email)?.Value;

    /// <summary>
    /// Gets the client ID from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The client ID, or null if not found.</returns>
    public static string? GetClientId(this ClaimsPrincipal principal) =>
        principal.FindFirst(IdentityConstants.ClaimTypes.ClientId)?.Value;

    /// <summary>
    /// Gets the display name from the claims principal.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>The display name.</returns>
    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        var name = principal.FindFirst(IdentityConstants.ClaimTypes.Name)?.Value;
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        var givenName = principal.GetGivenName();
        var familyName = principal.GetFamilyName();
        return $"{givenName} {familyName}".Trim();
    }

    /// <summary>
    /// Checks if the user has a specific access claim value.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <param name="accessValue">The access value to check for.</param>
    /// <returns>True if the user has the specified access.</returns>
    public static bool HasAccess(this ClaimsPrincipal principal, string accessValue) =>
        principal.HasClaim(IdentityConstants.AccessClaim, accessValue);

    /// <summary>
    /// Gets all access claim values for the user.
    /// </summary>
    /// <param name="principal">The claims principal.</param>
    /// <returns>All access claim values.</returns>
    public static IEnumerable<string> GetAccessValues(this ClaimsPrincipal principal) =>
        principal.FindAll(IdentityConstants.AccessClaim).Select(c => c.Value);
}
