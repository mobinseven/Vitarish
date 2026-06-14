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
    /// Optional combined text used by semantic search fields. Exact, lexical, and fuzzy search keep using Title and Body.
    /// </summary>
    public string? SemanticText { get; init; }

    /// <summary>
    /// Tenant that owns this document. Used as a mandatory filter on every search query
    /// so that results are always scoped to the requesting tenant (Option B isolation).
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Document type used for filtering (e.g. "project", "event").
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Optional URL to navigate to when a result is selected.
    /// </summary>
    public string? NavigationUrl { get; init; }

    /// <summary>
    /// Logical index that produced this document when returned from search.
    /// </summary>
    public string? IndexName { get; init; }

    /// <summary>
    /// Provider relevance score assigned by the search backend.
    /// </summary>
    public double? Score { get; init; }

    /// <summary>
    /// Arbitrary extra fields supplied by the provider.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
