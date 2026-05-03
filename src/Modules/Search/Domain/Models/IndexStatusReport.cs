namespace Vitarish.Search.Domain.Models;

/// <summary>
/// Snapshot of indexing statistics for a single logical search index.
/// </summary>
public record IndexStatusReport
{
    /// <summary>
    /// Logical index name as registered by the provider (e.g. "sea.projects").
    /// </summary>
    public required string IndexName { get; init; }

    /// <summary>
    /// Total number of documents currently indexed (primary shards only).
    /// </summary>
    public long DocumentCount { get; init; }

    /// <summary>
    /// On-disk store size in bytes (primary shards only).
    /// </summary>
    public long StoreSizeBytes { get; init; }
}
