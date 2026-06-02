namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Rich declaration used by <see cref="Abstractions.ISearchIndexProvider"/> to mark exact-match-first classes.
/// </summary>
public record SearchExactMatchPolicy
{
    public static SearchExactMatchPolicy Disabled { get; } = new();

    /// <summary>
    /// Enables exact-match-first protection for the provider/index.
    /// </summary>
    public bool IsExactMatchFirst { get; init; } = false;

    /// <summary>
    /// Elasticsearch field names that should be protected aggressively for exact matching.
    /// Example: "title.keyword", "id.keyword".
    /// </summary>
    public IReadOnlyList<string> FieldNames { get; init; } = [];

    /// <summary>
    /// Relative priority tier used by ranking/governance strategies.
    /// </summary>
    public SearchPriorityTier PriorityTier { get; init; } = SearchPriorityTier.Normal;

    public SearchExactMatchPolicy Normalize()
    {
        IReadOnlyList<string> normalizedFields = [.. FieldNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)];

        return !IsExactMatchFirst || normalizedFields.Count == 0 ? Disabled : (this with { FieldNames = normalizedFields });
    }
}
