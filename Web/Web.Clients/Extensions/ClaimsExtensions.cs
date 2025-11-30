using Duende.IdentityModel;
using System.Security.Claims;
using Vitarish.Web.Clients.Contracts;

namespace Vitarish.Web.Clients.Extensions;

/// <summary>
/// Extension methods for working with claims.
/// </summary>
public static class ClaimsExtensions
{
    /// <summary>
    /// Gets the user ID from a ClaimsPrincipal.
    /// </summary>
    public static string? GetId(this ClaimsPrincipal principal) =>
        principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

    /// <summary>
    /// Gets the user ID from a collection of ClaimDto.
    /// </summary>
    public static string? GetId(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == "sub")?.Value;

    /// <summary>
    /// Gets the given name from a collection of ClaimDto.
    /// </summary>
    public static string? GetGivenName(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;

    /// <summary>
    /// Gets the surname from a collection of ClaimDto.
    /// </summary>
    public static string? GetSurname(this IEnumerable<ClaimDto> claims) =>
        claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;

    /// <summary>
    /// Gets the given name from a ClaimsPrincipal.
    /// </summary>
    public static string? GetGivenName(this ClaimsPrincipal principal) =>
        principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;

    /// <summary>
    /// Gets the surname from a ClaimsPrincipal.
    /// </summary>
    public static string? GetSurname(this ClaimsPrincipal principal) =>
        principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;
}
