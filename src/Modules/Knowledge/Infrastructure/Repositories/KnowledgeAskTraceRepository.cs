using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeAskTraceRepository(KnowledgeDbContext dbContext) : IKnowledgeAskTraceRepository
{
    public async Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentException.ThrowIfNullOrWhiteSpace(trace.TenantId);
        bool corpusExists = await dbContext.Corpora.AnyAsync(c => c.Id == trace.CorpusId && c.TenantId == trace.TenantId, cancellationToken);
        if (!corpusExists) throw new InvalidOperationException($"Knowledge corpus '{trace.CorpusId}' does not exist for tenant '{trace.TenantId}'.");
        dbContext.AskTraces.Add(new KnowledgeAskTraceEntity { Id = trace.Id, CorpusId = trace.CorpusId, TenantId = trace.TenantId, Question = trace.Question, RetrievedChunkIdsJson = JsonSerializer.Serialize(trace.RetrievedChunkIds, KnowledgeImportRules.JsonOptions), GeneratedAnswer = trace.GeneratedAnswer, MissingInformation = trace.MissingInformation, CitationsJson = trace.CitationsJson, TraceKind = trace.TraceKind, IsAnswered = trace.IsAnswered });
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
