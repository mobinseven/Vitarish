using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Search.Domain.Options;

namespace Vitarish.Search.Infrastructure.Services;

/// <summary>
/// Elasticsearch-backed implementation of <see cref="ISearchService"/>.
/// Auto-discovers all registered <see cref="ISearchIndexProvider"/> instances via DI.
/// </summary>
internal sealed class SearchService(
    ElasticsearchClient client,
    IEnumerable<ISearchIndexProvider> providers,
    ElasticSearchOptions options) : ISearchService
{
    public async Task IndexAsync(
        string indexName,
        IEnumerable<SearchDocument> documents,
        CancellationToken cancellationToken = default)
    {
        string fullIndexName = BuildIndexName(indexName);

        List<IBulkOperation> operations = [.. documents.Select(doc =>
            new BulkIndexOperation<SearchDocument>(doc) { Id = doc.Id, Index = fullIndexName })];

        if (operations.Count == 0)
        {
            return;
        }

        BulkRequest request = new(fullIndexName) { Operations = new BulkOperationsCollection(operations) };

        BulkResponse response = await client.BulkAsync(request, cancellationToken);

        if (response.Errors)
        {
            IEnumerable<string> failed = response.Items
                .Where(i => i.Error is not null)
                .Select(i => i.Id ?? "unknown");

            throw new InvalidOperationException(
                $"Bulk index to '{fullIndexName}' had errors for documents: {string.Join(", ", failed)}");
        }
    }

    public async Task<SearchResult> SearchAsync(
        string indexName,
        string query,
        SearchOptions searchOptions,
        CancellationToken cancellationToken = default)
    {
        string fullIndexName = BuildIndexName(indexName);

        Query esQuery = BuildQuery(query, searchOptions);

        SearchRequest<SearchDocument> request = new(fullIndexName)
        {
            Query = esQuery,
            From = searchOptions.Skip,
            Size = searchOptions.Limit
        };

        SearchResponse<SearchDocument> response = await client.SearchAsync<SearchDocument>(request, cancellationToken);

        IReadOnlyList<SearchDocument> items = [.. response.Hits
            .Where(h => h.Source is not null)
            .Select(h => h.Source!)];

        return new SearchResult
        {
            Items = items,
            TotalCount = response.Total,
            Query = query
        };
    }

    public async Task RebuildIndexAsync(string indexName, CancellationToken cancellationToken = default)
    {
        ISearchIndexProvider? provider = providers.FirstOrDefault(p =>
            string.Equals(p.IndexName, indexName, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
        {
            throw new InvalidOperationException(
                $"No ISearchIndexProvider registered for index '{indexName}'.");
        }

        IReadOnlyList<SearchDocument> documents = await provider.GetDocumentsAsync(cancellationToken);

        await IndexAsync(indexName, documents, cancellationToken);
    }

    private string BuildIndexName(string indexName) =>
        string.IsNullOrWhiteSpace(options.DefaultIndexPrefix)
            ? indexName
            : $"{options.DefaultIndexPrefix}{indexName}";

    private static Query BuildQuery(string query, SearchOptions searchOptions)
    {
        Query textQuery = new MultiMatchQuery
        {
            Query = query,
            Fields = Fields.FromStrings(["title^3", "body"])
        };

        if (string.IsNullOrWhiteSpace(searchOptions.Type))
        {
            return textQuery;
        }

        return new BoolQuery
        {
            Must = [textQuery],
            Filter = [new TermQuery { Field = new Field("type.keyword"), Value = searchOptions.Type }]
        };
    }
}
