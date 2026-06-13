using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Retrieval;

public sealed class SearchKnowledgeRetriever(ISearchService search) : IKnowledgeRetriever
{
    public async Task<IReadOnlyList<KnowledgeHit>> RetrieveAsync(KnowledgeQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.Question);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.TenantId);

        SearchResult result = await search.SearchAsync(KnowledgeSearchDocumentContract.IndexName, query.Question, new SearchOptions
        {
            Limit = query.TopK,
            Type = KnowledgeSearchDocumentContract.DocumentType,
            TenantId = query.TenantId,
            CorpusId = query.CorpusId,
            MetadataFilters = query.Filters ?? new Dictionary<string, string>()
        }, cancellationToken);

        return [.. result.Items.Select(KnowledgeSearchDocumentContract.ToKnowledgeHit)];
    }
}
