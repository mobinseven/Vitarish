using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Application.Retrieval;

public sealed class SearchKnowledgeRetriever(ISearchService search) : IKnowledgeRetriever
{
    public const string IndexName = "knowledge-chunks";

    public async Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Question);

        SearchResult result = await search.SearchAsync(IndexName, query.Question, new SearchOptions
        {
            Limit = query.TopK,
            Type = "knowledge-chunk",
            TenantId = query.TenantId,
            CorpusId = query.CorpusId,
            MetadataFilters = query.Filters ?? new Dictionary<string, string>()
        }, cancellationToken);

        return [.. result.Items
            .Select((document, index) => new KnowledgeHit
            {
                ChunkId = document.Id,
                SourceId = MetadataString(document, "sourceId"),
                DocumentId = MetadataString(document, "documentId"),
                Text = document.Body ?? document.Title,
                Title = document.Title,
                Score = 1.0d / (index + 1)
            })];
    }

    private static string MetadataString(SearchDocument document, string key) => document.Metadata.TryGetValue(key, out object? found) ? found?.ToString() ?? string.Empty : string.Empty;
}
