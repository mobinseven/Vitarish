using System.Globalization;

namespace Vitarish.Main.Common;

/// <summary>
/// Central route API for all Razor pages and navigation helpers.
/// Conventions:
///   - All routes are lowercase kebab-case.
///   - Resource names are plural  (/users, /patients, /projects).
///   - IDs before operations     (/projects/{id}/edit, not /projects/edit/{id}).
///   - Nested resources explicit  (/patients/{id}/procedures/{id}/events/{id}).
///   - All ID segments constrained ({projectId:guid}, {dayOfWeek:int}).
///   - Query strings for view state (search, filters, sort, paging, return URLs).
///   - /r/{code} and /qr/{code} reserved for indirect short/QR links.
/// </summary>
public static partial class AppRoutes
{
    public const string Home = "/";

    /// <summary>Well-known query-string key constants.</summary>
    public static class QueryKeys
    {
        public const string Q = "q";
        public const string Page = "page";
        public const string PageSize = "pageSize";
        public const string Sort = "sort";
        public const string ReturnUrl = "returnUrl";
        public const string RequestedPath = "requestedPath";
        public const string Source = "source";
        public const string Campaign = "campaign";
        public const string Ref = "ref";
        public const string Scope = "scope";
    }

    // ── private path helpers ──────────────────────────────────────────────────

    private static string GuidValue(Guid value) =>
        value.ToString("D");

    private static string IntValue(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string SlugValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Path segment cannot be null or whitespace.", nameof(value));
        return Uri.EscapeDataString(value);
    }

    private static string CodeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Path segment cannot be null or whitespace.", nameof(value));
        return Uri.EscapeDataString(value);
    }
}
