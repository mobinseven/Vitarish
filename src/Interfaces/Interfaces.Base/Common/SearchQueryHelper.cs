using Microsoft.AspNetCore.Components;

namespace Vitarish.Interfaces.Base.Common;

public static class SearchQueryHelper
{
    public static string? ExtractQuery(NavigationManager navigationManager, string uri)
    {
        Uri absolute = navigationManager.ToAbsoluteUri(uri);
        string queryString = absolute.Query;
        if (string.IsNullOrEmpty(queryString) || queryString.Length <= 1)
        {
            return null;
        }

        foreach (string segment in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            if (!string.Equals(segment[..separatorIndex], "q", StringComparison.Ordinal))
            {
                continue;
            }

            string value = segment[(separatorIndex + 1)..];
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }

        return null;
    }
}
