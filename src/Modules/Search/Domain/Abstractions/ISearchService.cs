using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Domain.Abstractions;

/// <summary>
/// Shared search capability exposed to all modules and estates.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Indexes a batch of documents into the specified index.
    /// </summary>
    Task IndexAsync(string indexName, IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries the specified index and returns matching documents.
    /// </summary>
    Task<SearchResult> SearchAsync(string indexName, string query, SearchOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds the index for <paramref name="indexName"/> by fetching all documents
    /// from the matching <see cref="ISearchIndexProvider"/> and re-indexing them.
    /// </summary>
    Task RebuildIndexAsync(string indexName, CancellationToken cancellationToken = default);
}
