using System.Security.Claims;

namespace Vitarish.Main.Mock.Authentication;

internal static class PreviewAuthenticationDefaults
{
    internal const string UserName = "preview-user";
    internal const string Subject = "preview-user";

    internal static readonly Claim[] Claims =
    [
        new Claim(ClaimTypes.Name, UserName),
        new Claim("sub", Subject),
    ];

    internal static ClaimsPrincipal CreatePrincipal(string authenticationType)
    {
        ClaimsIdentity identity = new(Claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }
}
