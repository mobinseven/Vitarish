using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Application.Abstractions;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Knowledge.Infrastructure.Data;

namespace Vitarish.Knowledge.Infrastructure.Services.Diagnostics;

public sealed class KnowledgeDiagnosticsService(KnowledgeDbContext dbContext, IKnowledgeRetriever retriever, IKnowledgeContextBuilder contextBuilder, IKnowledgeAnswerer answerer) : IKnowledgeDiagnosticsService
{
    private const int PreviewLength = 240;
    private const string IndexProbe = "the";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<KnowledgeCorpusDiagnostics>> GetCorporaDiagnosticsAsync(CancellationToken cancellationToken)
    {
        return await CorpusDiagnosticsQuery()
            .OrderBy(corpus => corpus.TenantId)
            .ThenBy(corpus => corpus.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<KnowledgeCorpusDiagnostics> GetCorpusDiagnosticsAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await CorpusDiagnosticsQuery()
            .Where(corpus => corpus.CorpusId == corpusId && corpus.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException($"Knowledge corpus '{corpusId}' was not found for tenant '{tenantId}'.");
    }

    public async Task<IReadOnlyList<KnowledgeSourceDiagnostics>> GetSourcesAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return await dbContext.Sources.AsNoTracking()
            .Where(source => source.CorpusId == corpusId && source.TenantId == tenantId)
            .OrderByDescending(source => source.ImportedAt)
            .Select(source => new KnowledgeSourceDiagnostics
            {
                SourceId = source.Id,
                CorpusId = source.CorpusId,
                TenantId = source.TenantId,
                Name = source.Name,
                SourceType = source.SourceType,
                OriginalLocation = source.OriginalLocation,
                Version = source.Version,
                ImportedAt = source.ImportedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeChunkDiagnostics>> GetChunksAsync(string corpusId, string tenantId, KnowledgeChunkDiagnosticsQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<KnowledgeChunkEntity> chunks = dbContext.Chunks.AsNoTracking().Where(chunk => chunk.CorpusId == corpusId && chunk.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(query.SourceId)) chunks = chunks.Where(chunk => chunk.SourceId == query.SourceId);
        if (!string.IsNullOrWhiteSpace(query.DocumentId)) chunks = chunks.Where(chunk => chunk.DocumentId == query.DocumentId);
        if (!string.IsNullOrWhiteSpace(query.HeadingContains)) chunks = chunks.Where(chunk => chunk.HeadingPath != null && chunk.HeadingPath.Contains(query.HeadingContains));
        if (!string.IsNullOrWhiteSpace(query.TextContains)) chunks = chunks.Where(chunk => chunk.Text.Contains(query.TextContains));
        int limit = Math.Clamp(query.Limit, 1, 500);
        return await chunks.OrderBy(chunk => chunk.SourceId).ThenBy(chunk => chunk.DocumentId).ThenBy(chunk => chunk.Order)
            .Take(limit)
            .Select(chunk => new KnowledgeChunkDiagnostics
            {
                ChunkId = chunk.Id,
                SourceId = chunk.SourceId,
                DocumentId = chunk.DocumentId,
                Title = chunk.Title ?? string.Empty,
                HeadingPath = chunk.HeadingPath,
                Order = chunk.Order,
                TextLength = chunk.Text.Length,
                TextPreview = chunk.Text.Length <= PreviewLength ? chunk.Text : chunk.Text.Substring(0, PreviewLength),
                Text = chunk.Text
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeAskTraceDiagnostics>> GetAskTracesAsync(string corpusId, string tenantId, KnowledgeAskTraceDiagnosticsQuery query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<KnowledgeAskTraceEntity> traces = dbContext.AskTraces.AsNoTracking().Where(trace => trace.CorpusId == corpusId && trace.TenantId == tenantId);
        if (query.IsAnswered is not null) traces = traces.Where(trace => trace.IsAnswered == query.IsAnswered);
        if (!string.IsNullOrWhiteSpace(query.TraceKind)) traces = traces.Where(trace => trace.TraceKind == query.TraceKind);
        if (!string.IsNullOrWhiteSpace(query.QuestionContains)) traces = traces.Where(trace => trace.Question.Contains(query.QuestionContains));
        if (query.From is not null) traces = traces.Where(trace => trace.CreatedAt >= query.From);
        if (query.To is not null) traces = traces.Where(trace => trace.CreatedAt <= query.To);
        List<KnowledgeAskTraceEntity> rows = await traces.OrderByDescending(trace => trace.CreatedAt).Take(Math.Clamp(query.Limit, 1, 500)).ToListAsync(cancellationToken);
        return [.. rows.Select(MapTraceDiagnostics)];
    }

    public async Task<KnowledgeAskDebugResult> DebugAskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.CorpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);
        if (request.TopK < 1) throw new ArgumentOutOfRangeException(nameof(request), request.TopK, "TopK must be at least 1.");
        int chunkCount = await dbContext.Chunks.AsNoTracking().CountAsync(chunk => chunk.CorpusId == request.CorpusId && chunk.TenantId == request.TenantId, cancellationToken);
        if (chunkCount == 0) return EmptyDebugResult(request, KnowledgeFailureStage.NoImportedChunks, "Corpus has no imported chunks.", "No chunks exist for this corpus and tenant.");
        IReadOnlyList<KnowledgeHit> hits = await retriever.RetrieveAsync(new KnowledgeQuery { CorpusId = request.CorpusId, TenantId = request.TenantId, Question = request.Question, TopK = request.TopK }, cancellationToken);
        if (hits.Count == 0) return EmptyDebugResult(request, KnowledgeFailureStage.Retrieval, "Search returned no chunks.", "Search returned no hits.");
        KnowledgeContext context = contextBuilder.Build(request.Question, hits);
        KnowledgeAnswer answer = await answerer.AnswerFromHitsAsync(request with { SaveTrace = false, TraceKind = "diagnostic" }, hits, cancellationToken);
        KnowledgeFailureStage stage = KnowledgeFailureClassifier.Classify(chunkCount, hits, context, answer);
        return new KnowledgeAskDebugResult { CorpusId = request.CorpusId, TenantId = request.TenantId, Question = request.Question, RetrievedHits = hits, ContextChunkIds = context.ChunkIds, Answer = answer, FailureStage = stage, FailureReason = answer.MissingInformation };
    }

    public async Task<KnowledgeIndexDiagnostics> GetIndexDiagnosticsAsync(string corpusId, string tenantId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(corpusId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        int chunkCount = await dbContext.Chunks.AsNoTracking().CountAsync(chunk => chunk.CorpusId == corpusId && chunk.TenantId == tenantId, cancellationToken);
        IReadOnlyList<KnowledgeHit> hits = chunkCount == 0 ? [] : await retriever.RetrieveAsync(new KnowledgeQuery { CorpusId = corpusId, TenantId = tenantId, Question = IndexProbe, TopK = 1 }, cancellationToken);
        DateTimeOffset? lastImportedAt = await dbContext.Sources.AsNoTracking().Where(source => source.CorpusId == corpusId && source.TenantId == tenantId).MaxAsync(source => (DateTimeOffset?)source.ImportedAt, cancellationToken);
        return new KnowledgeIndexDiagnostics { CorpusId = corpusId, TenantId = tenantId, StoredChunkCount = chunkCount, SearchHitCountForProbe = hits.Count, LastIndexedAt = lastImportedAt, LooksIndexed = chunkCount > 0 && hits.Count > 0 };
    }

    private IQueryable<KnowledgeCorpusDiagnostics> CorpusDiagnosticsQuery() => dbContext.Corpora.AsNoTracking()
        .Select(corpus => new KnowledgeCorpusDiagnostics
        {
            CorpusId = corpus.Id,
            TenantId = corpus.TenantId,
            Name = corpus.Name,
            IsActive = corpus.IsActive,
            SourceCount = dbContext.Sources.Count(source => source.CorpusId == corpus.Id && source.TenantId == corpus.TenantId),
            DocumentCount = dbContext.Documents.Count(document => document.CorpusId == corpus.Id && document.TenantId == corpus.TenantId),
            ChunkCount = dbContext.Chunks.Count(chunk => chunk.CorpusId == corpus.Id && chunk.TenantId == corpus.TenantId),
            QuestionCount = dbContext.Questions.Count(question => question.CorpusId == corpus.Id),
            AskTraceCount = dbContext.AskTraces.Count(trace => trace.CorpusId == corpus.Id && trace.TenantId == corpus.TenantId),
            LastImportedAt = dbContext.Sources.Where(source => source.CorpusId == corpus.Id && source.TenantId == corpus.TenantId).Max(source => (DateTimeOffset?)source.ImportedAt),
            LastAskedAt = dbContext.AskTraces.Where(trace => trace.CorpusId == corpus.Id && trace.TenantId == corpus.TenantId).Max(trace => (DateTimeOffset?)trace.CreatedAt)
        });

    private static KnowledgeAskTraceDiagnostics MapTraceDiagnostics(KnowledgeAskTraceEntity trace)
    {
        JsonParseResult<string> retrievedChunkIds = DeserializeList<string>(trace.RetrievedChunkIdsJson, "retrieved chunk IDs");
        JsonParseResult<KnowledgeCitation> citations = DeserializeList<KnowledgeCitation>(trace.CitationsJson, "citations");
        return new KnowledgeAskTraceDiagnostics
        {
            TraceId = trace.Id,
            CorpusId = trace.CorpusId,
            TenantId = trace.TenantId,
            Question = trace.Question,
            IsAnswered = trace.IsAnswered,
            MissingInformation = trace.MissingInformation,
            RetrievedChunkIds = retrievedChunkIds.Items,
            RetrievedChunkIdsParseError = retrievedChunkIds.Error,
            GeneratedAnswer = trace.GeneratedAnswer,
            Citations = citations.Items,
            CitationsParseError = citations.Error,
            TraceKind = trace.TraceKind,
            CreatedAt = trace.CreatedAt
        };
    }

    private static KnowledgeAskDebugResult EmptyDebugResult(KnowledgeAskRequest request, KnowledgeFailureStage stage, string missingInformation, string reason) => new()
    {
        CorpusId = request.CorpusId,
        TenantId = request.TenantId,
        Question = request.Question,
        RetrievedHits = [],
        ContextChunkIds = [],
        Answer = new KnowledgeAnswer { Answer = string.Empty, IsAnswered = false, MissingInformation = missingInformation, Citations = [], RetrievedContext = [] },
        FailureStage = stage,
        FailureReason = reason
    };

    private static JsonParseResult<T> DeserializeList<T>(string json, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json)) return new JsonParseResult<T>([], $"Stored {fieldName} JSON was empty.");
        try
        {
            return new JsonParseResult<T>(JsonSerializer.Deserialize<IReadOnlyList<T>>(json, JsonOptions) ?? [], null);
        }
        catch (JsonException ex)
        {
            return new JsonParseResult<T>([], $"Stored {fieldName} JSON could not be parsed: {ex.Message}");
        }
    }

    private sealed record JsonParseResult<T>(IReadOnlyList<T> Items, string? Error);
}
