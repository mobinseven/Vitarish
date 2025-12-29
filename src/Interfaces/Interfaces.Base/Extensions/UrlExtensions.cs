namespace Vitarish.Interfaces.Base.Extensions;

public static class UrlExtensions
{
    public static string UrlCombine(string baseUrl, string relativePath) => string.IsNullOrWhiteSpace(baseUrl)
            ? relativePath ?? string.Empty
            : string.IsNullOrWhiteSpace(relativePath)
            ? baseUrl
            : new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), relativePath.TrimStart()).ToString();

    public static string AddQueryParam(this string url, string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        string fragment = string.Empty;
        int fragmentIndex = url.IndexOf('#', StringComparison.Ordinal);
        if (fragmentIndex >= 0)
        {
            fragment = url[fragmentIndex..];
            url = url[..fragmentIndex];
        }

        string basePart = url;
        string query = string.Empty;
        int queryIndex = url.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            basePart = url[..queryIndex];
            query = url[(queryIndex + 1)..];
        }

        List<KeyValuePair<string, string>> parameters = new();
        bool replaced = false;
        foreach (string entry in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('=', 2);
            string paramKey = parts[0];
            string paramValue = parts.Length > 1 ? parts[1] : string.Empty;

            if (string.Equals(paramKey, key, StringComparison.Ordinal))
            {
                parameters.Add(new KeyValuePair<string, string>(paramKey, Uri.EscapeDataString(value)));
                replaced = true;
                continue;
            }

            parameters.Add(new KeyValuePair<string, string>(paramKey, paramValue));
        }

        if (!replaced)
        {
            parameters.Add(new KeyValuePair<string, string>(Uri.EscapeDataString(key), Uri.EscapeDataString(value)));
        }

        string newQuery = string.Join("&", parameters.Select(pair => $"{pair.Key}={pair.Value}"));
        return $"{basePart}?{newQuery}{fragment}";
    }
}
