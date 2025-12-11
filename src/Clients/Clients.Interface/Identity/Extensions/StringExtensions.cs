namespace Vitarish.Clients.Interface.Identity.Extensions
{
    public static class StringExtensions
    {
        public static bool ContainsInvariant(this IEnumerable<string> items, string item, StringComparison comparisonType = StringComparison.InvariantCultureIgnoreCase)
        {
            ArgumentNullException.ThrowIfNull(items);

            ArgumentNullException.ThrowIfNull(item);

            return items.Any(x => string.Equals(x, item, comparisonType));
        }
    }
}
