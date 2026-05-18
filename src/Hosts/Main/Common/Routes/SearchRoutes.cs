using Microsoft.AspNetCore.WebUtilities;

namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Search
    {
        public const string Global = "/search";
        public const string ScopedTemplate = "/search/{scope}";
        public const string PlatformAdminRoute = "/platformadmin/search";

        /// <summary>Generates <c>/search</c> or <c>/search?q={query}</c>.</summary>
        public static string GlobalSearch(string? query = null) =>
            string.IsNullOrWhiteSpace(query)
                ? Global
                : QueryHelpers.AddQueryString(Global, QueryKeys.Q, query);

        /// <summary>Generates <c>/search/{scope}</c> or <c>/search/{scope}?q={query}</c>.</summary>
        public static string Scoped(string scope, string? query = null)
        {
            string url = $"{Global}/{SlugValue(scope)}";
            return string.IsNullOrWhiteSpace(query)
                ? url
                : QueryHelpers.AddQueryString(url, QueryKeys.Q, query);
        }

        /// <summary>Generates <c>/admin/search</c> or <c>/admin/search?q={query}</c>.</summary>
        public static string AdminSearch(string? query = null) =>
            string.IsNullOrWhiteSpace(query)
                ? PlatformAdminRoute
                : QueryHelpers.AddQueryString(PlatformAdminRoute, QueryKeys.Q, query);
    }
}
