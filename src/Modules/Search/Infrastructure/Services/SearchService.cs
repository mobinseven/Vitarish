using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
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
    IOptions<ElasticSearchOptions> options) : ISearchService
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
        string? indexName,
        string query,
        SearchOptions searchOptions,
        CancellationToken cancellationToken = default)
    {
        SearchOptions normalizedOptions = NormalizeOptions(searchOptions);
        string[] logicalIndexNames = ResolveLogicalIndexNames(indexName);

        if (logicalIndexNames.Length == 0 || string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult
            {
                Items = [],
                TotalCount = 0,
                Query = query
            };
        }

        Dictionary<string, string> fullToLogicalIndexNames = logicalIndexNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(BuildIndexName, logicalName => logicalName, StringComparer.OrdinalIgnoreCase);

        Indices indices = fullToLogicalIndexNames.Keys.ToArray();

        Query esQuery = BuildQuery(query, normalizedOptions);

        SearchRequest<SearchDocument> request = new(indices)
        {
            Query = esQuery,
            From = normalizedOptions.Skip,
            Size = normalizedOptions.Limit
        };

        SearchResponse<SearchDocument> response = await client.SearchAsync<SearchDocument>(request, cancellationToken);

        IReadOnlyList<SearchDocument> items = [.. response.Hits
            .Where(h => h.Source is not null)
            .Select(h => h.Source! with { IndexName = ResolveLogicalIndexName(h.Index?.ToString(), fullToLogicalIndexNames) })];

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

    public async Task<IReadOnlyList<IndexStatusReport>> GetIndexStatusAsync(
        CancellationToken cancellationToken = default)
    {
        string[] logicalNames = providers.Select(p => p.IndexName).ToArray();

        if (logicalNames.Length == 0)
        {
            return [];
        }

        string[] fullNames = logicalNames.Select(BuildIndexName).ToArray();
        Indices indices = fullNames;

        IndicesStatsResponse statsResponse = await client.Indices.StatsAsync(
            indices,
            [CommonStatsFlag.Docs, CommonStatsFlag.Store],
            cancellationToken);

        IReadOnlyDictionary<string, IndicesStats> indexMap =
            statsResponse.IsValidResponse && statsResponse.Indices is not null
                ? statsResponse.Indices
                : new Dictionary<string, IndicesStats>();

        List<IndexStatusReport> reports = [];

        foreach (string logicalName in logicalNames)
        {
            string fullName = BuildIndexName(logicalName);

            long docCount = 0;
            long storeBytes = 0;

            if (indexMap.TryGetValue(fullName, out IndicesStats? stats) && stats.Primaries is not null)
            {
                docCount = stats.Primaries.Docs?.Count ?? 0;
                storeBytes = stats.Primaries.Store?.SizeInBytes ?? 0;
            }

            reports.Add(new IndexStatusReport
            {
                IndexName = logicalName,
                DocumentCount = docCount,
                StoreSizeBytes = storeBytes
            });
        }

        return reports;
    }

    private string[] ResolveLogicalIndexNames(string? indexName)
    {
        if (!string.IsNullOrWhiteSpace(indexName))
        {
            return [indexName.Trim()];
        }

        return [.. providers
            .Select(p => p.IndexName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private static SearchOptions NormalizeOptions(SearchOptions searchOptions)
    {
        const int defaultLimit = 20;
        const int maxLimit = 100;

        int limit = searchOptions.Limit <= 0
            ? defaultLimit
            : Math.Min(searchOptions.Limit, maxLimit);

        return searchOptions with
        {
            Skip = Math.Max(0, searchOptions.Skip),
            Limit = limit
        };
    }

    private static string? ResolveLogicalIndexName(
        string? fullIndexName,
        IReadOnlyDictionary<string, string> fullToLogicalIndexNames)
    {
        if (string.IsNullOrWhiteSpace(fullIndexName))
        {
            return null;
        }

        return fullToLogicalIndexNames.TryGetValue(fullIndexName, out string? logicalIndexName)
            ? logicalIndexName
            : fullIndexName;
    }

    private string BuildIndexName(string indexName) =>
        string.IsNullOrWhiteSpace(options.Value.DefaultIndexPrefix)
            ? indexName
            : $"{options.Value.DefaultIndexPrefix}{indexName}";

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
