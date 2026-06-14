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

    /// <summary>
    /// Optional diagnostic summary of the retrieval strategies used.
    /// </summary>
    public SearchDiagnostics? Diagnostics { get; init; }
}

/// <summary>
/// Lightweight diagnostic summary for a search operation.
/// </summary>
public sealed record SearchDiagnostics
{
    /// <summary>
    /// Retrieval mode used to build the query.
    /// </summary>
    public required SearchRetrievalMode RetrievalMode { get; init; }

    /// <summary>
    /// Names of the retrieval strategies applied.
    /// </summary>
    public IReadOnlyList<string> AppliedStrategies { get; init; } = [];

    /// <summary>
    /// Optional provider-specific diagnostic values.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();
}
