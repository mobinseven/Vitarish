using System.Net;

namespace Vitarish.Authentication.Domain.ReturnUrls;

public static class ReturnUrlPolicy
{
    public static bool IsSafeLocal(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return true;

        string decoded = WebUtility.UrlDecode(returnUrl);

        int queryOrFragmentIndex = decoded.IndexOfAny(new[] { '?', '#' });
        string path = queryOrFragmentIndex >= 0 ? decoded[..queryOrFragmentIndex] : decoded;

        return path.StartsWith("/", StringComparison.Ordinal)
            && !path.StartsWith("//", StringComparison.Ordinal)
            && !path.StartsWith("/\\", StringComparison.Ordinal)
            && !path.StartsWith("/api/modules/authentication/", StringComparison.Ordinal)
            && path is not "/"
            && path is not "/login"
            && path is not "/logout"
            && !path.StartsWith("/login/", StringComparison.Ordinal)
            && !path.StartsWith("/logout/", StringComparison.Ordinal)
            && !decoded.Contains('\\');
    }

    public static ReturnUrl NormalizeOrDefault(string? returnUrl, string fallback = "/")
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return new(fallback);

        return new(IsSafeLocal(returnUrl) ? WebUtility.UrlDecode(returnUrl) : fallback);
    }
}
