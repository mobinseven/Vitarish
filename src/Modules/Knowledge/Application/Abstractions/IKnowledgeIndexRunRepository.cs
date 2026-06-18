using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeIndexRunRepository
{
    Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken);
    Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
}
