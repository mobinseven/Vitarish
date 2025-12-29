namespace Vitarish.Interfaces.Base.Extensions;

internal static class UrlHelper
{
    public static string UrlCombine(string baseUrl, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return relativePath ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return baseUrl;
        }

        Uri baseUri = new(baseUrl.TrimEnd('/') + "/");
        Uri combined = new(baseUri, relativePath.TrimStart('/'));
        UriBuilder builder = new(combined);
        builder.Query = EncodeQuery(builder.Query);

        return builder.Uri.AbsoluteUri;
    }

    private static string EncodeQuery(string query)
    {
        string trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        string[] parts = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        IEnumerable<string> encoded = parts.Select(part =>
        {
            string[] keyValue = part.Split('=', 2);
            string key = Uri.EscapeDataString(Uri.UnescapeDataString(keyValue[0]));
            string value = keyValue.Length > 1
                ? Uri.EscapeDataString(Uri.UnescapeDataString(keyValue[1]))
                : string.Empty;
            return $"{key}={value}";
        });

        return string.Join("&", encoded);
    }
}
