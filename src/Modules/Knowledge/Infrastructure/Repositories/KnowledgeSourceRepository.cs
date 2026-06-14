using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeSourceRepository(KnowledgeDbContext dbContext) : IKnowledgeSourceRepository
{
    public async Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await dbContext.Sources.AsNoTracking().Where(s => s.CorpusId == corpusId && s.TenantId == tenantId).OrderByDescending(s => s.ImportedAt).Select(s => s.ToModel()).ToListAsync(cancellationToken); }

    public async Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await dbContext.Sources.AsNoTracking().AnyAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken); }

    public async Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        var source = await dbContext.Sources.FirstOrDefaultAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{corpusId}' for tenant '{tenantId}'.");
        Guid[] chunkIds = await dbContext.Chunks.AsNoTracking().Where(c => c.SourceId == source.Id && c.CorpusId == corpusId && c.TenantId == tenantId).Select(c => c.Id).ToArrayAsync(cancellationToken);
        dbContext.Sources.Remove(source);
        await KnowledgeQuestionRemover.RemoveQuestionsForChunksAsync(dbContext, corpusId, chunkIds, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
