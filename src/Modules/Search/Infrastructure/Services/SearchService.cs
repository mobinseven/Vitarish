using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Search.Domain.Options;

namespace Vitarish.Search.Infrastructure.Services;

/// <summary>
/// Elasticsearch-backed implementation of <see cref="ISearchService"/>.
/// Auto-discovers all registered <see cref="ISearchIndexProvider"/> instances via DI.
/// </summary>
internal sealed class SearchService : ISearchService
{
    private readonly ElasticsearchClient client;
    private readonly IReadOnlyList<ISearchIndexProvider> providers;
    private readonly ElasticSearchOptions options;
    private readonly IRequestContext requestContext;

    public SearchService(
        ElasticsearchClient client,
        IEnumerable<ISearchIndexProvider> providers,
        IOptions<ElasticSearchOptions> options,
        IRequestContext requestContext,
        ILogger<SearchService> logger)
    {
        this.client = client;
        this.providers = [.. providers];
        this.options = options.Value;
        this.requestContext = requestContext;

        WarnIfNativeInferenceIsUnavailable(logger);
    }

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

        BulkRequest request = new(fullIndexName) { Operations = [.. operations] };

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


    public async Task DeleteAsync(
        string indexName,
        SearchDeleteOptions deleteOptions,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        ArgumentNullException.ThrowIfNull(deleteOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(deleteOptions.TenantId);

        string fullIndexName = BuildIndexName(indexName);
        Query deleteQuery = BuildDeleteQuery(deleteOptions);

        DeleteByQueryResponse response = await client.DeleteByQueryAsync<SearchDocument>(
            fullIndexName,
            request => request.Query(deleteQuery),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Delete from index '{fullIndexName}' failed.");
        }
    }

    public async Task<SearchResult> SearchAsync(
        string? indexName,
        string query,
        SearchOptions searchOptions,
        CancellationToken cancellationToken = default)
    {
        SearchOptions normalizedOptions = searchOptions.Normalize();
        string[] logicalIndexNames = ResolveLogicalIndexNames(indexName);

        if (logicalIndexNames.Length == 0 || string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult
            {
                Items = [],
                TotalCount = 0,
                Query = query,
                Diagnostics = BuildDiagnostics(normalizedOptions)
            };
        }

        Dictionary<string, string> fullToLogicalIndexNames = logicalIndexNames
            .ToDictionary(BuildIndexName, logicalName => logicalName, StringComparer.OrdinalIgnoreCase);

        Indices indices = fullToLogicalIndexNames.Keys.ToArray();

        Query esQuery = BuildQuery(query, normalizedOptions, ResolveTenantId(normalizedOptions));

        SearchRequest<SearchDocument> request = new(indices)
        {
            Query = esQuery,
            From = normalizedOptions.Skip,
            Size = normalizedOptions.Limit,
            MinScore = normalizedOptions.MinScore
        };

        SearchResponse<SearchDocument> response = await client.SearchAsync<SearchDocument>(request, cancellationToken);

        IReadOnlyList<SearchDocument> items = [.. response.Hits
            .Where(h => h.Source is not null)
            .Select(h => h.Source! with { IndexName = ResolveLogicalIndexName(h.Index?.ToString(), fullToLogicalIndexNames), Score = h.Score })];

        return new SearchResult
        {
            Items = items,
            TotalCount = response.Total,
            Query = query,
            Diagnostics = BuildDiagnostics(normalizedOptions)
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

    public IReadOnlyList<SearchIndexPolicyReport> GetIndexPolicyReports() =>
    [
        .. providers
            .GroupBy(provider => provider.IndexName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                ISearchIndexProvider provider = group.First();
                SearchExactMatchPolicy policy = provider.ExactMatchPolicy.Normalize();

                return new SearchIndexPolicyReport
                {
                    IndexName = provider.IndexName,
                    ExactMatchPolicy = policy
                };
            })
            .OrderBy(report => report.IndexName, StringComparer.OrdinalIgnoreCase)
    ];

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
            [CommonStatsFlag.Store],
            cancellationToken);

        IReadOnlyDictionary<string, IndicesStats> indexMap =
            statsResponse.IsValidResponse && statsResponse.Indices is not null
                ? statsResponse.Indices
                : new Dictionary<string, IndicesStats>();

        string tenantId = ResolveTenantId();
        Query tenantFilter = new TermQuery { Field = new Field("tenantId.keyword"), Value = tenantId };

        List<IndexStatusReport> reports = [];

        foreach (string logicalName in logicalNames)
        {
            string fullName = BuildIndexName(logicalName);

            CountResponse countResponse = await client.CountAsync(
                new CountRequest(fullName) { Query = tenantFilter },
                cancellationToken);

            long docCount = countResponse.IsValidResponse ? countResponse.Count : 0;

            long storeBytes = 0;
            if (indexMap.TryGetValue(fullName, out IndicesStats? stats) && stats.Primaries is not null)
            {
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

    private string ResolveTenantId(SearchOptions? searchOptions = null) =>
        !string.IsNullOrWhiteSpace(searchOptions?.TenantId)
            ? searchOptions.TenantId
            : !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before performing search operations.");

    private string[] ResolveLogicalIndexNames(string? indexName) => !string.IsNullOrWhiteSpace(indexName)
            ? [indexName.Trim()]
            : [.. providers
            .Select(p => p.IndexName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    private static string? ResolveLogicalIndexName(
        string? fullIndexName,
        IReadOnlyDictionary<string, string> fullToLogicalIndexNames) => string.IsNullOrWhiteSpace(fullIndexName)
            ? null
            : fullToLogicalIndexNames.TryGetValue(fullIndexName, out string? logicalIndexName)
            ? logicalIndexName
            : fullIndexName;

    private string BuildIndexName(string indexName) =>
        string.IsNullOrWhiteSpace(options.DefaultIndexPrefix)
            ? indexName
            : $"{options.DefaultIndexPrefix}{indexName}";

    private void WarnIfNativeInferenceIsUnavailable(ILogger<SearchService> logger)
    {
        if (options.EnableSemanticRetrieval && !options.UseNativeInferenceOnly)
        {
            logger.LogWarning(
                "Search module only supports Elasticsearch native inference. " +
                "EnableSemanticRetrieval is set but UseNativeInferenceOnly is false, so semantic retrieval must remain disabled.");
        }

        if (options.EnableSemanticRetrieval && string.IsNullOrWhiteSpace(options.InferenceEndpointId))
        {
            logger.LogWarning(
                "Elasticsearch native inference endpoint is not configured (Vitarish:ElasticSearch:InferenceEndpointId). " +
                "Semantic retrieval should stay disabled until this endpoint is available.");
        }
    }


    private static Query BuildDeleteQuery(SearchDeleteOptions deleteOptions)
    {
        List<Query> filters =
        [
            new TermQuery { Field = new Field("tenantId.keyword"), Value = deleteOptions.TenantId }
        ];

        if (!string.IsNullOrWhiteSpace(deleteOptions.Type))
        {
            filters.Add(new TermQuery { Field = new Field("type.keyword"), Value = deleteOptions.Type });
        }

        if (!string.IsNullOrWhiteSpace(deleteOptions.CorpusId))
        {
            filters.Add(new TermQuery { Field = new Field("metadata.corpusId.keyword"), Value = deleteOptions.CorpusId });
        }

        foreach (KeyValuePair<string, string> metadataFilter in deleteOptions.MetadataFilters)
        {
            if (string.IsNullOrWhiteSpace(metadataFilter.Key) || string.IsNullOrWhiteSpace(metadataFilter.Value)) continue;
            filters.Add(new TermQuery { Field = new Field($"metadata.{metadataFilter.Key}.keyword"), Value = metadataFilter.Value });
        }

        return new BoolQuery { Filter = filters };
    }

    private SearchDiagnostics? BuildDiagnostics(SearchOptions searchOptions) => !searchOptions.Explain
        ? null
        : new SearchDiagnostics
        {
            RetrievalMode = searchOptions.RetrievalMode,
            AppliedStrategies = BuildAppliedStrategies(searchOptions.RetrievalMode, IsSemanticRetrievalAvailable()),
            Data = new Dictionary<string, object?>
            {
                ["semanticEnabled"] = IsSemanticRetrievalAvailable(),
                ["minScore"] = searchOptions.MinScore
            }
        };

    private bool IsSemanticRetrievalAvailable() =>
        options.EnableSemanticRetrieval &&
        options.UseNativeInferenceOnly &&
        !string.IsNullOrWhiteSpace(options.InferenceEndpointId);

    private static IReadOnlyList<string> BuildAppliedStrategies(SearchRetrievalMode retrievalMode, bool semanticAvailable) => retrievalMode switch
    {
        SearchRetrievalMode.Strict => ["exact", "lexical"],
        SearchRetrievalMode.Balanced => ["exact", "lexical", "fuzzy", "synonym"],
        SearchRetrievalMode.Broad when semanticAvailable => ["exact", "lexical", "fuzzy", "synonym", "semantic"],
        SearchRetrievalMode.Broad => ["exact", "lexical", "fuzzy", "synonym"],
        _ => ["exact", "lexical", "fuzzy", "synonym"]
    };

    private Query BuildQuery(string query, SearchOptions searchOptions, string tenantId)
    {
        List<Query> shouldQueries =
        [
            new MatchPhraseQuery { Field = new Field("title"), Query = query, Boost = 5 },
            new MatchPhraseQuery { Field = new Field("body"), Query = query, Boost = 2 },
            new MultiMatchQuery
            {
                Query = query,
                Fields = Fields.FromStrings(["title^3", "body"])
            },
            new TermQuery { Field = new Field("id.keyword"), Value = query, Boost = 10 },
            new TermQuery { Field = new Field("title.keyword"), Value = query, Boost = 8 }
        ];
        if (searchOptions.RetrievalMode is SearchRetrievalMode.Balanced or SearchRetrievalMode.Broad)
        {
            shouldQueries.Add(new MultiMatchQuery
            {
                Query = query,
                Fields = Fields.FromStrings(["title^2", "body"]),
                Fuzziness = new Fuzziness("AUTO"),
                PrefixLength = 1,
                MaxExpansions = 25
            });
            shouldQueries.Add(new MultiMatchQuery
            {
                Query = query,
                Fields = Fields.FromStrings(["title.synonym^2", "body.synonym"])
            });
        }
        if (searchOptions.RetrievalMode is SearchRetrievalMode.Broad && IsSemanticRetrievalAvailable())
        {
            shouldQueries.Add(new KnnQuery
            {
                Field = new Field("vector"),
                QueryVector = new float[] { /* TODO: Implementation dependent: vectorization of query string */ },
                Boost = 5
            });
        }

        List<Query> filters = BuildSearchFilters(searchOptions, tenantId);

        return new BoolQuery
        {
            Should = shouldQueries,
            MinimumShouldMatch = 1,
            Filter = filters
        };
    }

    private static List<Query> BuildSearchFilters(SearchOptions searchOptions, string tenantId)
    {
        List<Query> filters =
        [
            new TermQuery { Field = new Field("tenantId.keyword"), Value = tenantId }
        ];

        if (!string.IsNullOrWhiteSpace(searchOptions.Type))
        {
            filters.Add(new TermQuery { Field = new Field("type.keyword"), Value = searchOptions.Type });
        }

        if (!string.IsNullOrWhiteSpace(searchOptions.CorpusId))
        {
            filters.Add(new TermQuery { Field = new Field("metadata.corpusId.keyword"), Value = searchOptions.CorpusId });
        }

        foreach (KeyValuePair<string, string> metadataFilter in searchOptions.MetadataFilters)
        {
            if (string.IsNullOrWhiteSpace(metadataFilter.Key) || string.IsNullOrWhiteSpace(metadataFilter.Value)) continue;
            filters.Add(new TermQuery { Field = new Field($"metadata.{metadataFilter.Key}.keyword"), Value = metadataFilter.Value });
        }

        return filters;
    }
}
