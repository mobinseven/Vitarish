namespace Vitarish.Web.Clients.Extensions;

/// <summary>
/// Extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Checks if a collection contains a string using invariant comparison.
    /// </summary>
    public static bool ContainsInvariant(this IEnumerable<string> items, string item, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(item);

        return items.Any(x => string.Equals(x, item, comparisonType));
    }
}
