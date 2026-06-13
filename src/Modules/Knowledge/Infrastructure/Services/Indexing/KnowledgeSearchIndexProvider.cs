using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Services.Retrieval;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Infrastructure.Services.Indexing;

public sealed class KnowledgeSearchIndexProvider(IKnowledgeRepository repository) : ISearchIndexProvider
{
    public string IndexName => KnowledgeSearchDocumentContract.IndexName;

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<KnowledgeCorpus> corpora = await repository.ListCorporaAsync(cancellationToken);
        List<SearchDocument> documents = [];

        foreach (KnowledgeCorpus corpus in corpora.Where(static corpus => corpus.IsActive))
        {
            IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksAsync(corpus.Id, cancellationToken);
            documents.AddRange(chunks.Select(chunk => KnowledgeSearchDocumentContract.ToSearchDocument(corpus, chunk)));
        }

        return documents;
    }
}
