using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Domain.Abstractions;

/// <summary>
/// Extension point that any module or block implements to expose its data for indexing.
/// Register implementations in DI; they are auto-discovered by <see cref="ISearchService"/>.
/// </summary>
public interface ISearchIndexProvider
{
    /// <summary>
    /// Logical index name for this provider's data (e.g. "sea.projects").
    /// </summary>
    string IndexName { get; }

    /// <summary>
    /// Returns all documents that should be indexed by this provider.
    /// </summary>
    Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
