using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeChunkRepository(KnowledgeDbContext dbContext) : IKnowledgeChunkRepository
{
    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await OrderedChunks().Where(c => c.CorpusId == corpusId && c.TenantId == tenantId).Select(c => c.ToModel()).ToListAsync(cancellationToken); }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await OrderedChunks().Where(c => c.SourceId == sourceId && c.CorpusId == corpusId && c.TenantId == tenantId).Select(c => c.ToModel()).ToListAsync(cancellationToken); }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpusIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (corpusIds.Count == 0) return [];
        Guid[] ids = [.. corpusIds.Distinct()];
        return await OrderedChunks().Where(chunk => ids.Contains(chunk.CorpusId) && chunk.TenantId == tenantId).Select(chunk => chunk.ToModel()).ToListAsync(cancellationToken);
    }

    private IQueryable<KnowledgeChunkEntity> OrderedChunks() => dbContext.Chunks.AsNoTracking().OrderBy(c => c.SourceId).ThenBy(c => c.DocumentId).ThenBy(c => c.Order);
}
