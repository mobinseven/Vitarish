using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeIndexRunRepository(KnowledgeDbContext dbContext) : IKnowledgeIndexRunRepository
{
    public async Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(run.TenantId);
        bool corpusExists = await dbContext.Corpora.AnyAsync(c => c.Id == run.CorpusId && c.TenantId == run.TenantId, cancellationToken);
        if (!corpusExists) throw new InvalidOperationException($"Knowledge corpus '{run.CorpusId}' does not exist for tenant '{run.TenantId}'.");
        dbContext.IndexRuns.Add(new KnowledgeIndexRunEntity { Id = run.Id, CorpusId = run.CorpusId, TenantId = run.TenantId, IndexedChunkCount = run.IndexedChunkCount, Scope = run.Scope, SourceId = run.SourceId });
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { ArgumentException.ThrowIfNullOrWhiteSpace(tenantId); return await dbContext.IndexRuns.AsNoTracking().Where(r => r.CorpusId == corpusId && r.TenantId == tenantId).OrderByDescending(r => r.CreatedAt).Select(r => r.ToModel()).FirstOrDefaultAsync(cancellationToken); }
}
