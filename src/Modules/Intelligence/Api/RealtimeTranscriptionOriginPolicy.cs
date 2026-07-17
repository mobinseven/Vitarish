namespace Vitarish.Intelligence.Api;

public sealed class RealtimeTranscriptionOriginPolicy
{
    private readonly HashSet<string> _allowedOrigins;

    public RealtimeTranscriptionOriginPolicy(IEnumerable<string> allowedOrigins)
    {
        ArgumentNullException.ThrowIfNull(allowedOrigins);

        _allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string allowedOrigin in allowedOrigins)
        {
            if (!TryNormalizeOrigin(allowedOrigin, out string normalized))
            {
                throw new InvalidOperationException(
                    "Every trusted WebSocket origin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or wildcard.");
            }

            _allowedOrigins.Add(normalized);
        }
    }

    public bool IsTrusted(string? origin) =>
        TryNormalizeOrigin(origin, out string normalized)
        && _allowedOrigins.Contains(normalized);

    internal static bool TryNormalizeOrigin(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || value.Contains('*', StringComparison.Ordinal)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || uri.AbsolutePath != "/"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
