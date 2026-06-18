using Vitarish.Knowledge.Application.Contracts;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeIndexer
{
    Task<KnowledgeIndexResult> IndexCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<KnowledgeIndexResult> ReindexSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteFromIndexAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
}
