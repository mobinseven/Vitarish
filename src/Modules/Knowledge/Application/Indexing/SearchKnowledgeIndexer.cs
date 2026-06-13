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
        KnowledgeCorpus? corpus = await repository.GetCorpusAsync(corpusId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' is inactive.");

        IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksAsync(corpus.Id, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk))];
        await search.IndexAsync(KnowledgeSearchDocumentContract.IndexName, documents, cancellationToken);
        return new KnowledgeIndexResult(corpusId, documents.Length);
    }
}
