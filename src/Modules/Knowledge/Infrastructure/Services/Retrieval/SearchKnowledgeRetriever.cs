using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Retrieval;

public sealed class SearchKnowledgeRetriever(ISearchService search, IKnowledgeRetrievalPlanner planner) : IKnowledgeRetriever
{
    public async Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Question);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.TenantId);

        KnowledgeRetrievalPlan plan = await planner.PlanAsync(query.Question, cancellationToken);
        Dictionary<Guid, RankedKnowledgeHit> rankedHits = [];
        int firstSeenOrder = 0;

        string corpusIdStr = query.CorpusId.ToString("N");
        IReadOnlyDictionary<string, string> metadataFilters = query.Filters ?? new Dictionary<string, string>();
        SearchRetrievalMode retrievalMode = MapRetrievalMode(query.RetrievalMode);

        var searchTasks = plan.SearchQueries.Select(searchQuery =>
            search.SearchAsync(KnowledgeSearchDocumentContract.IndexName, searchQuery, new SearchOptions
            {
                Limit = query.TopK,
                Type = KnowledgeSearchDocumentContract.DocumentType,
                TenantId = query.TenantId,
                CorpusId = corpusIdStr,
                MetadataFilters = metadataFilters,
                RetrievalMode = retrievalMode,
                Explain = true
            }, cancellationToken)
        );

        SearchResult[] searchResults = await Task.WhenAll(searchTasks);

        for (int queryIndex = 0; queryIndex < searchResults.Length; queryIndex++)
        {
            SearchResult result = searchResults[queryIndex];
            foreach (KnowledgeHit hit in result.Items.Select(KnowledgeSearchDocumentContract.ToKnowledgeHit))
            {
                if (rankedHits.TryGetValue(hit.ChunkId, out RankedKnowledgeHit? existing))
                {
                    rankedHits[hit.ChunkId] = existing.RegisterMatch(hit, queryIndex);
                    continue;
                }

                rankedHits.Add(hit.ChunkId, RankedKnowledgeHit.First(hit, queryIndex, firstSeenOrder++));
            }
        }

        return [.. rankedHits.Values
            .OrderByDescending(hit => hit.QueryMatchCount)
            .ThenByDescending(hit => hit.BestScore)
            .ThenBy(hit => hit.FirstSeenOrder)
            .Take(query.TopK)
            .Select(hit => hit.Hit with { Score = hit.BestScore })];
    }

    private static SearchRetrievalMode MapRetrievalMode(KnowledgeRetrievalMode mode) => mode switch
    {
        KnowledgeRetrievalMode.Broad => SearchRetrievalMode.Broad,
        _ => SearchRetrievalMode.Balanced
    };

    private sealed record RankedKnowledgeHit(KnowledgeHit Hit, double BestScore, int FirstQueryPosition, int QueryMatchCount, int FirstSeenOrder)
    {
        public static RankedKnowledgeHit First(KnowledgeHit hit, int queryPosition, int firstSeenOrder) => new(hit, hit.Score, queryPosition, 1, firstSeenOrder);

        public RankedKnowledgeHit RegisterMatch(KnowledgeHit hit, int queryPosition) => this with
        {
            Hit = hit.Score > BestScore ? hit : Hit,
            BestScore = Math.Max(BestScore, hit.Score),
            FirstQueryPosition = Math.Min(FirstQueryPosition, queryPosition),
            QueryMatchCount = QueryMatchCount + 1
        };
    }
}
