using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Indexing;

public sealed class SearchKnowledgeIndexer(IKnowledgeCorpusRepository corpusRepository, IKnowledgeSourceRepository sourceRepository, IKnowledgeChunkRepository chunkRepository, IKnowledgeIndexRunRepository indexRunRepository, ISearchService search) : IKnowledgeIndexer
{
    public async Task<KnowledgeIndexResult> IndexCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
                ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        KnowledgeCorpus? corpus = await corpusRepository.GetCorpusAsync(corpusId, tenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' is inactive.");

        IReadOnlyList<KnowledgeChunk> chunks = await chunkRepository.GetChunksAsync(corpus.Id, tenantId, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk))];
        await search.IndexAsync(KnowledgeSearchDocumentContract.IndexName, documents, cancellationToken);
        await SaveRunAsync(corpusId, tenantId, documents.Length, "corpus", null, cancellationToken);
        return new KnowledgeIndexResult(corpusId, tenantId, documents.Length, "corpus", null);
    }

    public async Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        KnowledgeCorpus? corpus = await corpusRepository.GetCorpusAsync(corpusId, tenantId, cancellationToken);
        if (corpus is null) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist.");
        if (!corpus.IsActive) throw new InvalidOperationException($"Knowledge corpus '{corpusId}' is inactive.");

        bool sourceExists = await sourceRepository.SourceExistsAsync(sourceId, corpusId, tenantId, cancellationToken);
        if (!sourceExists) throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{corpusId}'.");

        IReadOnlyList<KnowledgeChunk> chunks = await chunkRepository.GetChunksBySourceAsync(sourceId, corpusId, tenantId, cancellationToken);
        SearchDocument[] documents = [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk))];
        await search.IndexAsync(KnowledgeSearchDocumentContract.IndexName, documents, cancellationToken);
        await SaveRunAsync(corpusId, tenantId, documents.Length, "source", sourceId, cancellationToken);
        return new KnowledgeIndexResult(corpusId, tenantId, documents.Length, "source", sourceId);
    }


    public async Task DeleteFromIndexAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        await search.DeleteAsync(KnowledgeSearchDocumentContract.IndexName, new SearchDeleteOptions
        {
            TenantId = tenantId,
            Type = KnowledgeSearchDocumentContract.DocumentType,
            CorpusId = corpusId.ToString("N"),
            MetadataFilters = new Dictionary<string, string>
            {
                [KnowledgeSearchDocumentContract.SourceIdMetadataKey] = sourceId.ToString("N")
            }
        }, cancellationToken);
    }

    private Task SaveRunAsync(Guid corpusId, string tenantId, int indexedChunkCount, string scope, Guid? sourceId, CancellationToken cancellationToken) =>
        indexRunRepository.SaveIndexRunAsync(new KnowledgeIndexRun
        {
            Id = Guid.NewGuid(),
            CorpusId = corpusId,
            TenantId = tenantId,
            IndexedChunkCount = indexedChunkCount,
            Scope = scope,
            SourceId = sourceId,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
}
