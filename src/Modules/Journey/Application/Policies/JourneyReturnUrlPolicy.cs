using System.Net;
using Vitarish.Journey.Application.Abstractions;

namespace Vitarish.Journey.Application.Policies;

public sealed class JourneyReturnUrlPolicy : IJourneyReturnUrlPolicy
{
    public string? Normalize(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;

        string decoded = WebUtility.UrlDecode(returnUrl);
        return IsSafeLocalPath(decoded) ? decoded : null;
    }

    private static bool IsSafeLocalPath(string value) =>
        value.StartsWith("/", StringComparison.Ordinal)
        && !value.StartsWith("//", StringComparison.Ordinal)
        && !value.StartsWith("/\\", StringComparison.Ordinal)
        && !value.Contains('\\');
}
