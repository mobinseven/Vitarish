using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeSourceRepository
{
    Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
}
