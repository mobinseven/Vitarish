using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Elasticsearch.Application.Abstractions;
using Vitarish.Modules.Elasticsearch.Application.Models;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

public class ElasticsearchQueryService(
    ElasticsearchClient client,
    ISearchIndexNameResolver indexNameResolver,
    ILogger<ElasticsearchQueryService> logger) : ISearchQueryService
{
    public async Task<SearchQueryResult> SearchAsync(string entityType, SearchQueryRequest request, CancellationToken cancellationToken = default)
    {
        var indexName = indexNameResolver.ResolveIndexName(entityType, request.TenantId);
        var from = Math.Max(0, (request.Page - 1) * request.PageSize);
        var size = Math.Max(1, request.PageSize);

        var query = string.IsNullOrWhiteSpace(request.SearchText)
            ? new Query(new MatchAllQuery())
            : new Query(new QueryStringQuery { Query = request.SearchText });

        var response = await client.SearchAsync<object>(descriptor => descriptor
            .Index(indexName)
            .From(from)
            .Size(size)
            .Query(query), cancellationToken);

        if (!response.IsValidResponse)
        {
            logger.LogError("Search failed for entity type {EntityType} in index {Index}.", entityType, indexName);
            throw new InvalidOperationException($"Search failed for entity type '{entityType}'.");
        }

        var hits = response.Hits.Select(hit => new SearchHit
        {
            Id = hit.Id ?? string.Empty,
            Score = hit.Score ?? 0,
            Highlights = hit.Highlight?.ToDictionary(
                item => item.Key,
                item => (IReadOnlyCollection<string>)item.Value.ToArray())
                ?? new Dictionary<string, IReadOnlyCollection<string>>()
        }).ToArray();

        return new SearchQueryResult
        {
            Total = response.HitsMetadata?.Total?.Value ?? hits.LongLength,
            Hits = hits
        };
    }
}
