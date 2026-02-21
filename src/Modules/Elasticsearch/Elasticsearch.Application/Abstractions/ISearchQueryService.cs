using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Application.Abstractions;

public interface ISearchQueryService
{
    Task<SearchQueryResult> SearchAsync(string entityType, SearchQueryRequest request, CancellationToken cancellationToken = default);
}
