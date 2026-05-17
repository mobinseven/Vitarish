namespace Vitarish.Links.Domain.Models;

/// <summary>
/// Shared rules for storing and resolving short links.
/// </summary>
public static class LinkRules
{
    public const string SlugCollation = "Latin1_General_100_CS_AS_SC_UTF8";

    public static string NormalizeDomain(string domain) => domain.Trim().ToLowerInvariant();

    public static string NormalizeDestinationUrl(string destinationUrl) => destinationUrl.Trim();

    public static bool IsValidDestinationUrl(string destinationUrl)
    {
        if (!Uri.TryCreate(destinationUrl, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }
}
