using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Domain.Abstractions;

/// <summary>
/// Shared search capability exposed to all modules and apps.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Indexes a batch of documents into the specified index.
    /// </summary>
    Task IndexAsync(string indexName, IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the specified index and returns matching documents.
    /// When <paramref name="indexName"/> is <see langword="null"/>, all registered indexes are searched.
    /// </summary>
    Task<SearchResult> SearchAsync(string? indexName, string query, SearchOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the index for <paramref name="indexName"/> by fetching all documents
    /// from the matching <see cref="ISearchIndexProvider"/> and re-indexing them.
    /// </summary>
    Task RebuildIndexAsync(string indexName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a status report for every registered index, including document count and store size.
    /// Indexes that do not yet exist in Elasticsearch are reported with zero counts.
    /// </summary>
    Task<IReadOnlyList<IndexStatusReport>> GetIndexStatusAsync(CancellationToken cancellationToken = default);
}
