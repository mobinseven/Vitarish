using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Domain.Mappers;
using Vitarish.Knowledge.Domain.Models;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Repositories;

public sealed class KnowledgeRepository(KnowledgeDbContext dbContext) : IKnowledgeRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveCorpusAsync(KnowledgeCorpus corpus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(corpus); Validate(corpus.Name); Validate(corpus.TenantId);
        KnowledgeCorpusEntity? existing = await dbContext.Corpora.FirstOrDefaultAsync(row => row.Id == corpus.Id && row.TenantId == corpus.TenantId, cancellationToken);
        if (existing is null) dbContext.Corpora.Add(new KnowledgeCorpusEntity { Id = corpus.Id, Name = corpus.Name, TenantId = corpus.TenantId, Description = corpus.Description, IsActive = corpus.IsActive, CreatedAt = DateTimeOffset.UtcNow });
        else { existing.Name = corpus.Name; existing.Description = corpus.Description; existing.IsActive = corpus.IsActive; }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<KnowledgeCorpus?> GetCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await CorpusQuery(corpusId, tenantId).Select(c => c.ToModel()).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListCorporaAsync(string tenantId, CancellationToken cancellationToken)
    { Validate(tenantId); return await dbContext.Corpora.AsNoTracking().Where(c => c.TenantId == tenantId).OrderBy(c => c.Name).Select(c => c.ToModel()).ToListAsync(cancellationToken); }

    public async Task<IReadOnlyList<KnowledgeCorpus>> ListActiveCorporaAsync(CancellationToken cancellationToken)
        => await dbContext.Corpora.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.TenantId).ThenBy(c => c.Name).Select(c => c.ToModel()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeSource>> GetSourcesAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await dbContext.Sources.AsNoTracking().Where(s => s.CorpusId == corpusId && s.TenantId == tenantId).OrderByDescending(s => s.ImportedAt).Select(s => s.ToModel()).ToListAsync(cancellationToken);

    public async Task<bool> SourceExistsAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await dbContext.Sources.AsNoTracking().AnyAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken);

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await OrderedChunks().Where(c => c.CorpusId == corpusId && c.TenantId == tenantId).Select(c => c.ToModel()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksBySourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await OrderedChunks().Where(c => c.SourceId == sourceId && c.CorpusId == corpusId && c.TenantId == tenantId).Select(c => c.ToModel()).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeChunk>> GetChunksForCorporaAsync(IReadOnlyCollection<Guid> corpusIds, CancellationToken cancellationToken)
    { ArgumentNullException.ThrowIfNull(corpusIds); if (corpusIds.Count == 0) return []; Guid[] ids = [.. corpusIds.Distinct()]; return await OrderedChunks().Where(c => ids.Contains(c.CorpusId)).Select(c => c.ToModel()).ToListAsync(cancellationToken); }

    public async Task<IReadOnlyList<KnowledgeQuestion>> GetQuestionsAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { KnowledgeCorpusEntity corpus = await RequireCorpusAsync(corpusId, tenantId, cancellationToken); return await dbContext.Questions.AsNoTracking().Where(q => q.CorpusEntityId == corpus.Id).OrderBy(q => q.Id).Select(q => q.ToModel()).ToListAsync(cancellationToken); }

    public async Task SaveImportAsync(KnowledgeImportResult result, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(result);
        KnowledgeCorpusEntity corpus = await RequireCorpusAsync(result.CorpusId, result.Sources.First().TenantId, cancellationToken);
        Dictionary<Guid, Guid> sourceIds = [];
        foreach (KnowledgeSource source in result.Sources)
        { KnowledgeSourceEntity entity = new() { Id = source.Id, CorpusEntityId = corpus.Id, CorpusId = source.CorpusId, TenantId = source.TenantId, Name = source.Name, SourceType = source.SourceType, OriginalLocation = source.OriginalLocation, Version = source.Version, Checksum = source.Checksum, ImportedAt = source.ImportedAt }; dbContext.Sources.Add(entity); sourceIds[source.Id] = entity.Id; }
        Dictionary<Guid, Guid> documentIds = [];
        foreach (KnowledgeDocument document in result.Documents)
        { KnowledgeDocumentEntity entity = new() { Id = document.Id, CorpusEntityId = corpus.Id, SourceEntityId = sourceIds[document.SourceId], CorpusId = document.CorpusId, TenantId = document.TenantId, SourceId = document.SourceId, Title = document.Title, Text = document.Text, Product = document.Product, Version = document.Version, Language = document.Language }; dbContext.Documents.Add(entity); documentIds[document.Id] = entity.Id; }
        dbContext.Chunks.AddRange(result.Chunks.Select(chunk => new KnowledgeChunkEntity { Id = chunk.Id, CorpusEntityId = corpus.Id, SourceEntityId = sourceIds[chunk.SourceId], DocumentEntityId = documentIds[chunk.DocumentId], CorpusId = chunk.CorpusId, TenantId = chunk.TenantId, SourceId = chunk.SourceId, DocumentId = chunk.DocumentId, Text = chunk.Text, Order = chunk.Order, Title = chunk.Title, HeadingPath = chunk.HeadingPath }));
        dbContext.Questions.AddRange(result.Questions.Select(q => new KnowledgeQuestionEntity { Id = q.Id, CorpusEntityId = corpus.Id, CorpusId = q.CorpusId, Question = q.Question, ExpectedAnswer = q.ExpectedAnswer, ExpectedChunkIdsJson = JsonSerializer.Serialize(q.ExpectedChunkIds, JsonOptions) }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSourceAsync(Guid sourceId, Guid corpusId, string tenantId, CancellationToken cancellationToken)
    { KnowledgeSourceEntity source = await dbContext.Sources.FirstOrDefaultAsync(s => s.Id == sourceId && s.CorpusId == corpusId && s.TenantId == tenantId, cancellationToken) ?? throw new InvalidOperationException($"Knowledge source '{sourceId}' does not exist in corpus '{corpusId}' for tenant '{tenantId}'."); Guid[] chunkIds = await dbContext.Chunks.AsNoTracking().Where(c => c.SourceEntityId == source.Id).Select(c => c.Id).ToArrayAsync(cancellationToken); dbContext.Sources.Remove(source); await RemoveQuestionsForChunksAsync(corpusId, chunkIds, cancellationToken); await dbContext.SaveChangesAsync(cancellationToken); }

    public async Task ReplaceSourceAsync(Guid sourceId, KnowledgeImportResult result, CancellationToken cancellationToken)
    { if (result.Sources.Single().Id != sourceId) throw new InvalidOperationException($"Replacement source id '{result.Sources.Single().Id}' must match requested source id '{sourceId}'."); await DeleteSourceAsync(sourceId, result.CorpusId, result.Sources.Single().TenantId, cancellationToken); await SaveImportAsync(result, cancellationToken); }

    public async Task SaveAskTraceAsync(KnowledgeAskTrace trace, CancellationToken cancellationToken)
    { KnowledgeCorpusEntity corpus = await RequireCorpusAsync(trace.CorpusId, trace.TenantId, cancellationToken); dbContext.AskTraces.Add(new KnowledgeAskTraceEntity { Id = trace.Id, CorpusEntityId = corpus.Id, CorpusId = trace.CorpusId, TenantId = trace.TenantId, Question = trace.Question, RetrievedChunkIdsJson = JsonSerializer.Serialize(trace.RetrievedChunkIds, JsonOptions), GeneratedAnswer = trace.GeneratedAnswer, MissingInformation = trace.MissingInformation, CitationsJson = trace.CitationsJson, TraceKind = trace.TraceKind, IsAnswered = trace.IsAnswered, CreatedAt = trace.CreatedAt }); await dbContext.SaveChangesAsync(cancellationToken); }

    public async Task SaveIndexRunAsync(KnowledgeIndexRun run, CancellationToken cancellationToken)
    { KnowledgeCorpusEntity corpus = await RequireCorpusAsync(run.CorpusId, run.TenantId, cancellationToken); dbContext.IndexRuns.Add(new KnowledgeIndexRunEntity { Id = run.Id, CorpusEntityId = corpus.Id, CorpusId = run.CorpusId, TenantId = run.TenantId, IndexedChunkCount = run.IndexedChunkCount, Scope = run.Scope, SourceId = run.SourceId, CreatedAt = run.CreatedAt }); await dbContext.SaveChangesAsync(cancellationToken); }

    public async Task<KnowledgeIndexRun?> GetLatestIndexRunAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken)
        => await dbContext.IndexRuns.AsNoTracking().Where(r => r.CorpusId == corpusId && r.TenantId == tenantId).OrderByDescending(r => r.CreatedAt).Select(r => r.ToModel()).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<KnowledgeCorpusEntity> CorpusQuery(Guid corpusId, string tenantId) { Validate(tenantId); return dbContext.Corpora.AsNoTracking().Where(c => c.Id == corpusId && c.TenantId == tenantId); }
    private IQueryable<KnowledgeChunkEntity> OrderedChunks() => dbContext.Chunks.AsNoTracking().OrderBy(c => c.SourceId).ThenBy(c => c.DocumentId).ThenBy(c => c.Order);
    private async Task<KnowledgeCorpusEntity> RequireCorpusAsync(Guid corpusId, string tenantId, CancellationToken cancellationToken) => await dbContext.Corpora.FirstOrDefaultAsync(c => c.Id == corpusId && c.TenantId == tenantId, cancellationToken) ?? throw new InvalidOperationException($"Knowledge corpus '{corpusId}' does not exist for tenant '{tenantId}'.");
    private async Task RemoveQuestionsForChunksAsync(Guid corpusId, IReadOnlyCollection<Guid> chunkIds, CancellationToken cancellationToken)
    { if (chunkIds.Count == 0) return; HashSet<Guid> chunkIdSet = new(chunkIds); KnowledgeQuestionEntity[] rows = [.. (await dbContext.Questions.Where(q => q.CorpusId == corpusId).ToListAsync(cancellationToken)).Where(q => EntityMapper.DeserializeStringList(q.ExpectedChunkIdsJson).Any(chunkIdSet.Contains))]; dbContext.Questions.RemoveRange(rows); }
    private static void Validate(string value) => ArgumentException.ThrowIfNullOrWhiteSpace(value);
}
