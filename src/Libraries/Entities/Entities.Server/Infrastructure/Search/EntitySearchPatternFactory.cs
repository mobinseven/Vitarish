namespace Vitarish.Entities.Server.Infrastructure.Search;

public sealed class EntitySearchPatternFactory
{
    private static readonly char[] SearchSeparators = [' ', '\t', '\r', '\n'];
    private readonly EntitySearchTextNormalizer textNormalizer;

    public EntitySearchPatternFactory(EntitySearchTextNormalizer textNormalizer)
    {
        this.textNormalizer = textNormalizer ?? throw new ArgumentNullException(nameof(textNormalizer));
    }

    public IReadOnlyCollection<string> CreateSearchPatterns(string search)
    {
        return [.. CreateSearchFragments(search).Select(static fragment => $"%{EscapeLikeFragment(fragment)}%")];
    }

    public IReadOnlyCollection<string> CreateSearchFragments(string search)
    {
        ArgumentNullException.ThrowIfNull(search);

        HashSet<string> fragments = new(StringComparer.Ordinal);

        foreach (string fragment in search.Split(SearchSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string normalized = textNormalizer.Normalize(fragment);
            if (normalized.Length > 0)
            {
                _ = fragments.Add(normalized);
            }
        }

        return [.. fragments];
    }

    private static string EscapeLikeFragment(string fragment) => fragment
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}
