using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Indexing;

public sealed class SearchKnowledgeIndexer(IKnowledgeRepository repository, ISearchService search) : IKnowledgeIndexer
{
    public async Task<KnowledgeIndexResult> IndexCorpusAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        KnowledgeCorpus? corpus = await repository.GetCorpusAsync(corpusId, tenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' is inactive.");

        IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksAsync(corpus.Id, tenantId, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk))];
        await search.IndexAsync(KnowledgeSearchDocumentContract.IndexName, documents, cancellationToken);
        return new KnowledgeIndexResult(corpusId, documents.Length);
    }
}
