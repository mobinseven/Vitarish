using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeSourceRepository(KnowledgeDbContext dbContext)
    : EntityRepository<KnowledgeSourceEntity, KnowledgeSourceDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeSourceEntity, KnowledgeSourceDescriptor>,
      IKnowledgeSourceRepository
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    public async Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await DbContext.Sources.AsNoTracking().Where(s => s.CorpusId == corpusId && s.TenantId == tenantId).OrderByDescending(s => s.ImportedAt).Select(s => s.ToModel()).ToListAsync(cancellationToken); }

    public async Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await DbContext.Sources.AsNoTracking().AnyAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken); }

    public async Task<bool> DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var source = await DbContext.Sources.FirstOrDefaultAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken);
        if (source is null)
        {
            return false;
        }

        KnowledgeDocumentEntity[] documents = await DbContext.Documents
            .Where(document => document.SourceId == source.Id && document.CorpusId == corpusId && document.TenantId == tenantId)
            .ToArrayAsync(cancellationToken);

        KnowledgeChunkEntity[] chunks = await DbContext.Chunks
            .Where(chunk => chunk.SourceId == source.Id && chunk.CorpusId == corpusId && chunk.TenantId == tenantId)
            .ToArrayAsync(cancellationToken);

        Guid[] chunkIds = [.. chunks.Select(chunk => chunk.Id)];

        DbContext.Chunks.RemoveRange(chunks);
        DbContext.Documents.RemoveRange(documents);
        DbContext.Sources.Remove(source);

        await KnowledgeQuestionRemover.RemoveQuestionsForChunksAsync(DbContext, corpusId, chunkIds, cancellationToken);
        int changed = await DbContext.SaveChangesAsync(cancellationToken);
        return changed > 0;
    }
}
