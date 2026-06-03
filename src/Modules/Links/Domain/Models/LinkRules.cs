namespace Vitarish.Links.Domain.Models;

/// <summary>
/// Shared rules for storing and resolving short links.
/// </summary>
public static class LinkRules
{
    public static string NormalizeDomain(string domain) => domain.Trim().ToLowerInvariant();

    public static string NormalizeDestinationUrl(string destinationUrl) => destinationUrl.Trim();

    public static bool IsValidDestinationUrl(string destinationUrl) => Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri? uri) && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
}
