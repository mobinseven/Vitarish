namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Provider-agnostic document representation used for indexing and search results.
/// </summary>
public record SearchDocument
{
    /// <summary>
    /// Unique identifier for the document within its index.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Primary text field — indexed with highest relevance weight.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Optional longer body text for full-text search.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Document type used for filtering (e.g. "project", "event").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Optional URL to navigate to when a result is selected.
    /// </summary>
    public string? NavigationUrl { get; init; }

    /// <summary>
    /// Arbitrary extra fields supplied by the provider.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
