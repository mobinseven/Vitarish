namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Envelope returned by a search operation.
/// </summary>
public record SearchResult
{
    /// <summary>
    /// Matched documents for the current page.
    /// </summary>
    public required IReadOnlyList<SearchDocument> Items { get; init; }

    /// <summary>
    /// Total number of matching documents across all pages.
    /// </summary>
    public required long TotalCount { get; init; }

    /// <summary>
    /// The query string that produced this result.
    /// </summary>
    public required string Query { get; init; }
}
