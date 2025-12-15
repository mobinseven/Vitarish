using Duende.IdentityModel;
using System.Security.Claims;

namespace Vitarish.Clients.Interface.Identity.Extensions;

public static class ClaimsExtensions
{
    public static string? GetId(this ClaimsPrincipal principal) => principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
    public static string? GetId(this IEnumerable<ClaimDto> claims) => claims.FirstOrDefault(c => c.Type == "sub")?.Value;
    public static string? GetGivenName(this IEnumerable<ClaimDto> claims) => claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;
    public static string? GetSurname(this IEnumerable<ClaimDto> claims) => claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;
    public static string? GetGivenName(this ClaimsPrincipal principal) => principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.GivenName)?.Value;
    public static string? GetSurname(this ClaimsPrincipal principal) => principal.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName)?.Value;
}
