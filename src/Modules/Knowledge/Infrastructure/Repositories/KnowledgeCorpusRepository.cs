using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeCorpusRepository(KnowledgeDbContext dbContext) : IKnowledgeCorpusRepository
{
    public async Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpus);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpus.TenantId);
        KnowledgeCorpusEntity? existing = await dbContext.Corpora.FirstOrDefaultAsync(row => row.Id == corpus.Id && row.TenantId == corpus.TenantId, cancellationToken);
        if (existing is null) dbContext.Corpora.Add(new KnowledgeCorpusEntity { Id = corpus.Id, Name = corpus.Name, TenantId = corpus.TenantId, Description = corpus.Description, IsActive = corpus.IsActive });
        else { existing.Name = corpus.Name; existing.Description = corpus.Description; existing.IsActive = corpus.IsActive; }
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Corpora.AsNoTracking().Where(c => c.Id == corpusId && c.TenantId == tenantId).Select(c => c.ToModel()).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Corpora.AsNoTracking().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).Select(c => c.ToModel()).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken)
        => await ActiveCorpora().Select(c => c.ToModel()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListAllActiveCorporaForIndexingAsync(CancellationToken cancellationToken)
        => await ActiveCorpora().IgnoreQueryFilters().Select(c => c.ToModel()).ToListAsync(cancellationToken);

    private IQueryable<KnowledgeCorpusEntity> ActiveCorpora()
        => dbContext.Corpora.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.TenantId).ThenBy(c => c.Name);
}
