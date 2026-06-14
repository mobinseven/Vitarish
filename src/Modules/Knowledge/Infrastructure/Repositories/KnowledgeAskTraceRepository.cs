using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Data;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeAskTraceRepository(KnowledgeDbContext dbContext)
    : EntityRepository<KnowledgeAskTraceEntity, KnowledgeAskTraceDescriptor, KnowledgeDbContext>(dbContext),
      IEntityRepository<KnowledgeAskTraceEntity, KnowledgeAskTraceDescriptor>,
      IKnowledgeAskTraceRepository
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    public async Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace.TenantId);
        bool corpusExists = await DbContext.Corpora.AnyAsync(c => c.Id == trace.CorpusId && c.TenantId == trace.TenantId, cancellationToken);
        if (!corpusExists) throw new InvalidOperationException($"Knowledge corpus '{trace.CorpusId}' does not exist for tenant '{trace.TenantId}'.");
        KnowledgeAskTraceEntity entity = new()
        {
            Id = trace.Id,
            CorpusId = trace.CorpusId,
            TenantId = trace.TenantId,
            Question = trace.Question,
            RetrievedChunkIdsJson = JsonSerializer.Serialize(trace.RetrievedChunkIds, KnowledgeImportRules.JsonOptions),
            GeneratedAnswer = trace.GeneratedAnswer,
            MissingInformation = trace.MissingInformation,
            CitationsJson = trace.CitationsJson,
            TraceKind = trace.TraceKind,
            IsAnswered = trace.IsAnswered
        };
        _ = await AddAsync(entity, cancellationToken);
    }
}
