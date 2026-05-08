namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Advertised policy metadata for one registered search index provider.
/// </summary>
public record SearchIndexPolicyReport
{
    /// <summary>
    /// Logical index name advertised by the provider (e.g. "sea.projects").
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Exact-match-first declaration for the provider.
    /// </summary>
    public required SearchExactMatchPolicy ExactMatchPolicy { get; init; }
}
