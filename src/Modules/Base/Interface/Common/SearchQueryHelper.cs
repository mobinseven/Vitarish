using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Net;

namespace Vitarish.Modules.Base.Interface.Common;

public static class SearchQueryHelper
{
    private const string QueryParameterName = "q";

    public static string? ExtractQuery(NavigationManager navigationManager, string uri)
    {
        Uri absolute = navigationManager.ToAbsoluteUri(uri);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(absolute.Query);

        return !query.TryGetValue(QueryParameterName, out StringValues values) || values.Count == 0 ? null : WebUtility.UrlDecode(values[0]);
    }
}
