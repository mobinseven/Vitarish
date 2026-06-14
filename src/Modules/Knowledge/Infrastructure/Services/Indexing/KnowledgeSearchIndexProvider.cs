using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Indexing;

public sealed class KnowledgeSearchIndexProvider(IKnowledgeCorpusRepository corpusRepository, IKnowledgeChunkRepository chunkRepository) : ISearchIndexProvider
{
    public string IndexName => KnowledgeSearchDocumentContract.IndexName;

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<KnowledgeCorpus> corpora = await corpusRepository.ListActiveCorporaAsync(cancellationToken);
        if (corpora.Count == 0) return [];

        Dictionary<Guid, KnowledgeCorpus> corpusById = corpora.ToDictionary(corpus => corpus.Id);
        List<SearchDocument> documents = [];
        foreach (IGrouping<string, KnowledgeCorpus> tenantCorpora in corpora.GroupBy(corpus => corpus.TenantId))
        {
            Guid[] corpusIds = [.. tenantCorpora.Select(corpus => corpus.Id)];
            IReadOnlyList<KnowledgeChunk> chunks = await chunkRepository.GetChunksForCorporaAsync(corpusIds, tenantCorpora.Key, cancellationToken);
            documents.AddRange(chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpusById[chunk.CorpusId], chunk)));
        }

        return documents;
    }
}
