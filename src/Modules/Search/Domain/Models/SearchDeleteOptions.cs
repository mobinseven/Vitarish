namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Exact filters for deleting documents from a search index.
/// </summary>
public record SearchDeleteOptions
{
    /// <summary>
    /// Tenant scope for the delete command.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Optional document type filter.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Optional corpus scope for knowledge documents.
    /// </summary>
    public string? CorpusId { get; init; }

    /// <summary>
    /// Exact-match metadata filters for the delete command.
    /// </summary>
    public IReadOnlyDictionary<string, string> MetadataFilters { get; init; } = new Dictionary<string, string>();
}
