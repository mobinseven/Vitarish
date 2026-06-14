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
        await SaveRunAsync(corpusId, tenantId, documents.Length, "corpus", null, cancellationToken);
        return new KnowledgeIndexResult(corpusId, tenantId, documents.Length, "corpus", null);
    }

    public async Task<KnowledgeIndexResult> ReindexSourceAsync(string sourceId, string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        KnowledgeCorpus? corpus = await repository.GetCorpusAsync(corpusId, tenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' is inactive.");

        bool sourceExists = (await repository.GetSourcesAsync(corpusId, tenantId, cancellationToken)).Any(source => string.Equals(source.Id, sourceId, StringComparison.Ordinal));
        if (!sourceExists) throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{corpusId}'.");

        IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksBySourceAsync(sourceId, corpusId, tenantId, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk))];
        await search.IndexAsync(KnowledgeSearchDocumentContract.IndexName, documents, cancellationToken);
        await SaveRunAsync(corpusId, tenantId, documents.Length, "source", sourceId, cancellationToken);
        return new KnowledgeIndexResult(corpusId, tenantId, documents.Length, "source", sourceId);
    }

    private Task SaveRunAsync(string corpusId, string tenantId, int indexedChunkCount, string scope, string? sourceId, CancellationToken cancellationToken) =>
        repository.SaveIndexRunAsync(new KnowledgeIndexRun
        {
            Id = Guid.NewGuid().ToString("n"),
            CorpusId = corpusId,
            TenantId = tenantId,
            IndexedChunkCount = indexedChunkCount,
            Scope = scope,
            SourceId = sourceId,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
}
