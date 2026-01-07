using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Vitarish.Interfaces.Base.Common;

public static class SearchQueryHelper
{
    private const string QueryParameterName = "q";

    public static string? ExtractQuery(NavigationManager navigationManager, string uri)
    {
        Uri absolute = navigationManager.ToAbsoluteUri(uri);
        var query = QueryHelpers.ParseQuery(absolute.Query);

        if (!query.TryGetValue(QueryParameterName, out var values) || values.Count == 0)
        {
            return null;
        }

        return WebUtility.UrlDecode(values[0]);
    }
}
