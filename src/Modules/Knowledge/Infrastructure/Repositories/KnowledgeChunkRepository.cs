using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeChunkRepository(KnowledgeDbContext dbContext)
    : EntityRepository<KnowledgeChunkEntity, KnowledgeChunkDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeChunkEntity, KnowledgeChunkDescriptor>,
      IKnowledgeChunkRepository
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

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
        return await ChunksForCorpora(ids, tenantId).Select(chunk => chunk.ToModel()).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaForIndexingAsync(IReadOnlyCollection<Guid> corpusIds, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpusIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (corpusIds.Count == 0) return [];
        Guid[] ids = [.. corpusIds.Distinct()];
        return await ChunksForCorpora(ids, tenantId).IgnoreQueryFilters().Select(chunk => chunk.ToModel()).ToListAsync(cancellationToken);
    }

    private IQueryable<KnowledgeChunkEntity> ChunksForCorpora(IReadOnlyCollection<Guid> corpusIds, string tenantId)
        => OrderedChunks().Where(chunk => corpusIds.Contains(chunk.CorpusId) && chunk.TenantId == tenantId);

    private IQueryable<KnowledgeChunkEntity> OrderedChunks() => DbContext.Chunks.AsNoTracking().OrderBy(c => c.SourceId).ThenBy(c => c.DocumentId).ThenBy(c => c.Order);
}
