namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Pagination and filtering options for a search operation.
/// </summary>
public record SearchOptions
{
    /// <summary>
    /// Number of documents to skip (for pagination).
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Maximum number of documents to return.
    /// </summary>
    public int Limit { get; init; } = 20;

    /// <summary>
    /// Optional document type filter (e.g. "project").
    /// When null, all types are returned.
    /// </summary>
    public string? Type { get; init; }
}
