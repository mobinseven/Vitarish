using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeCorpusRepository
{
    Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken);
    Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeCorpus>> ListAllActiveCorporaForIndexingAsync(CancellationToken cancellationToken);
}
