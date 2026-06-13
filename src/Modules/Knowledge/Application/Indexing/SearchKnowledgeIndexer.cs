using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Retrieval;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Application.Indexing;

public sealed class SearchKnowledgeIndexer(IKnowledgeRepository repository, ISearchService search) : IKnowledgeIndexer
{
    public async Task<KnowledgeIndexResult> IndexCorpusAsync(string corpusId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksAsync(corpusId, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => new SearchDocument
        {
            Id = chunk.Id,
            Title = chunk.Title ?? chunk.DocumentId,
            Body = chunk.Text,
            TenantId = chunk.TenantId ?? throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' is missing tenant scope."),
            Type = "knowledge-chunk",
            IndexName = SearchKnowledgeRetriever.IndexName,
            Metadata = new Dictionary<string, object?>
            {
                ["corpusId"] = chunk.CorpusId,
                ["documentId"] = chunk.DocumentId,
                ["sourceId"] = chunk.SourceId,
                ["order"] = chunk.Order,
                ["headingPath"] = chunk.HeadingPath
            }
        })];
        await search.IndexAsync(SearchKnowledgeRetriever.IndexName, documents, cancellationToken);
        return new KnowledgeIndexResult(corpusId, documents.Length);
    }
}
