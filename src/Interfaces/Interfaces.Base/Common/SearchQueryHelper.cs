using Microsoft.AspNetCore.Components;
using System.Net;

namespace Vitarish.Interfaces.Base.Common;

public static class SearchQueryHelper
{
    private const string QueryParameterName = "q";

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

            if (!string.Equals(segment[..separatorIndex], QueryParameterName, StringComparison.Ordinal))
            {
                continue;
            }

            string value = segment[(separatorIndex + 1)..];
            return WebUtility.UrlDecode(value);
        }

        return null;
    }
}
