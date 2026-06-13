using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Indexing;

public sealed class KnowledgeSearchIndexProvider(IKnowledgeRepository repository) : ISearchIndexProvider
{
    public string IndexName => KnowledgeSearchDocumentContract.IndexName;

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<KnowledgeCorpus> corpora = await repository.ListActiveCorporaAsync(cancellationToken);
        if (corpora.Count == 0)
        {
            return [];
        }

        Dictionary<string, KnowledgeCorpus> corpusById = corpora.ToDictionary(corpus => corpus.Id, StringComparer.Ordinal);
        IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksForCorporaAsync(corpusById.Keys.ToArray(), cancellationToken);

        return [.. chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpusById[chunk.CorpusId], chunk))];
    }
}
