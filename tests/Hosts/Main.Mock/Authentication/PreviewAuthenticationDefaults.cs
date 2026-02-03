using System.Security.Claims;
using Vitarish.Interfaces.Accounts.Constants;

namespace Vitarish.Main.Mock.Authentication;

internal static class PreviewAuthenticationDefaults
{
    internal const string UserName = "preview-user";
    internal const string Subject = "preview-user";

    internal static readonly Claim[] Claims =
    [
        new Claim(ClaimTypes.Name, UserName),
        new Claim("sub", Subject),
        new Claim(AaasConstants.AccessClaim, "admin.local:::Admin")
    ];

    internal static ClaimsPrincipal CreatePrincipal(string authenticationType)
    {
        ClaimsIdentity identity = new(Claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }
}
