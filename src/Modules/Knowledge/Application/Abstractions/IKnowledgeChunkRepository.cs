using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeChunkRepository
{
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaForIndexingAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken);
}
