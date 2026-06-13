using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Application.Retrieval;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Knowledge.Application.Indexing;

public sealed class KnowledgeSearchIndexProvider(IKnowledgeRepository repository) : ISearchIndexProvider
{
    public string IndexName => SearchKnowledgeRetriever.IndexName;

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<KnowledgeCorpus> corpora = await repository.ListCorporaAsync(cancellationToken);
        List<SearchDocument> documents = [];

        foreach (KnowledgeCorpus corpus in corpora.Where(static corpus => corpus.IsActive))
        {
            IReadOnlyList<KnowledgeChunk> chunks = await repository.GetChunksAsync(corpus.Id, cancellationToken);
            documents.AddRange(chunks.Select(static chunk => new SearchDocument
            {
                Id = chunk.Id,
                Title = chunk.Title ?? chunk.HeadingPath ?? chunk.Id,
                Body = chunk.Text,
                TenantId = chunk.TenantId ?? throw new InvalidOperationException($"Knowledge chunk '{chunk.Id}' is missing tenant scope."),
                Type = "knowledge-chunk",
                IndexName = SearchKnowledgeRetriever.IndexName,
                Metadata = new Dictionary<string, object?>
                {
                    ["corpusId"] = chunk.CorpusId,
                    ["sourceId"] = chunk.SourceId,
                    ["documentId"] = chunk.DocumentId,
                    ["headingPath"] = chunk.HeadingPath,
                    ["order"] = chunk.Order
                }
            }));
        }

        return documents;
    }
}
