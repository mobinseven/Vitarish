namespace Vitarish.Contents.Domain.Models;

public static class ContentHrefRules
{
    public static bool IsAllowed(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl) || value.Any(char.IsWhiteSpace) || value.Contains('\\'))
        {
            return false;
        }

        if (value.StartsWith("/", StringComparison.Ordinal))
        {
            return !value.StartsWith("//", StringComparison.Ordinal);
        }

        return value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
            string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(uri.Host) &&
            string.IsNullOrEmpty(uri.UserInfo);
    }
}
