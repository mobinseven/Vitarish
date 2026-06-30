using System.Net;

namespace Vitarish.Authentication.Domain.ReturnUrls;

public static class ReturnUrlPolicy
{
    public static bool IsSafeLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return true;

        string decoded = WebUtility.UrlDecode(returnUrl);
        return decoded.StartsWith("/", StringComparison.Ordinal)
            && !decoded.StartsWith("//", StringComparison.Ordinal)
            && !decoded.StartsWith("/\\", StringComparison.Ordinal)
            && !decoded.StartsWith("/api/modules/authentication/", StringComparison.Ordinal)
            && decoded is not "/login"
            && decoded is not "/logout"
            && !decoded.StartsWith("/login/", StringComparison.Ordinal)
            && !decoded.Contains('\\');
    }

    public static ReturnUrl NormalizeOrDefault(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return new(fallback);

        return new(IsSafeLocal(returnUrl) ? WebUtility.UrlDecode(returnUrl) : fallback);
    }
}
