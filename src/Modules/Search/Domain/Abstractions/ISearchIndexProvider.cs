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
    /// Optional exact-match-first declaration for this provider.
    /// Modules can use this rich policy (field list + priority tier) to protect precision-sensitive classes.
    /// </summary>
    SearchExactMatchPolicy ExactMatchPolicy => SearchExactMatchPolicy.Disabled;

    /// <summary>
    /// Returns all documents that should be indexed by this provider.
    /// </summary>
    Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default);
}
