using System.Net;
using Vitarish.Journey.Application.Abstractions;

namespace Vitarish.Journey.Infrastructure.ReturnUrl;

/// <summary>
/// Centralizes return URL validation and normalization.
/// Rules:
/// - Only local platform paths are valid.
/// - External URLs are invalid.
/// - Protocol-relative URLs are invalid.
/// - Backslash-relative URLs are invalid.
/// - Encoded bypass attempts are decoded then checked.
/// - Invalid return URLs are ignored, not repaired.
/// - Authentication-only routes are not valid post-authentication destinations.
/// </summary>
public sealed class JourneyReturnUrlPolicy : IJourneyReturnUrlPolicy
{
    public bool IsSafe(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return false;

        string decoded = WebUtility.UrlDecode(returnUrl);
        return IsSafeLocalPath(decoded);
    }

    public string? Normalize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        string decoded = WebUtility.UrlDecode(returnUrl);
        return IsSafeLocalPath(decoded) ? decoded : null;
    }

    private static bool IsSafeLocalPath(string value) =>
        value.StartsWith("/", StringComparison.Ordinal)
        && !value.StartsWith("//", StringComparison.Ordinal)
        && !value.StartsWith("/\\", StringComparison.Ordinal)
        && !value.StartsWith("/api/modules/authentication/", StringComparison.Ordinal)
        && value is not "/"
        && value is not "/login"
        && value is not "/logout"
        && !value.StartsWith("/login/", StringComparison.Ordinal)
        && !value.Contains('\\')
        && !value.Contains('?')
        && !value.Contains('#');
}
