using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Contracts;
using Vitarish.Knowledge.Application.Queries;
using Vitarish.Knowledge.Infrastructure.Data;
using Vitarish.Knowledge.Infrastructure.Extensions;

namespace Vitarish.Knowledge.Infrastructure.Handlers;

public sealed class KnowledgeUserWorkspaceQueryHandler(KnowledgeDbContext dbContext) : IRequestHandler<KnowledgeUserWorkspaceQuery, KnowledgeUserWorkspaceReadModel>
{
    public async Task<KnowledgeUserWorkspaceReadModel> Handle(KnowledgeUserWorkspaceQuery request, CancellationToken cancellationToken)
    {
        string? tenantError = KnowledgeOwnerInputValidator.ValidateTenant(request.TenantId);
        if (tenantError is not null || string.IsNullOrWhiteSpace(request.UserId))
        {
            return KnowledgeUserWorkspaceReadModel.Empty;
        }

        string tenantId = request.TenantId!;
        var corpora = await dbContext.Corpora.AsNoTracking()
            .Where(corpus => corpus.TenantId == tenantId && corpus.IsActive)
            .OrderBy(corpus => corpus.Name)
            .Select(corpus => new { corpus.Id, corpus.Name, corpus.Description, corpus.IsActive })
            .ToArrayAsync(cancellationToken);

        if (corpora.Length == 0)
        {
            return KnowledgeUserWorkspaceReadModel.Empty;
        }

        Guid[] corpusIds = corpora.Select(corpus => corpus.Id).ToArray();
        Dictionary<Guid, int> sourceCounts = await dbContext.Sources.AsNoTracking()
            .Where(source => source.TenantId == tenantId && corpusIds.Contains(source.CorpusId))
            .GroupBy(source => source.CorpusId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);

        Dictionary<Guid, int> chunkCounts = await (
                from source in dbContext.Sources.AsNoTracking()
                join document in dbContext.Documents.AsNoTracking() on source.Id equals document.SourceId
                join chunk in dbContext.Chunks.AsNoTracking() on document.Id equals chunk.DocumentId
                where source.TenantId == tenantId && corpusIds.Contains(source.CorpusId)
                group chunk by source.CorpusId into corpusChunks
                select new { CorpusId = corpusChunks.Key, Count = corpusChunks.Count() })
            .ToDictionaryAsync(row => row.CorpusId, row => row.Count, cancellationToken);

        return new KnowledgeUserWorkspaceReadModel(corpora
            .Select(corpus => new KnowledgeUserCorpusReadModel(
                corpus.Id,
                corpus.Name,
                corpus.Description,
                corpus.IsActive,
                sourceCounts.GetValueOrDefault(corpus.Id),
                chunkCounts.GetValueOrDefault(corpus.Id)))
            .ToArray());
    }
}
