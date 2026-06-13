namespace Vitarish.Knowledge.Application.Abstractions;

public interface IKnowledgeDiagnosticsService
{
    Task<IReadOnlyList<KnowledgeCorpusDiagnostics>> GetCorporaDiagnosticsAsync(CancellationToken cancellationToken);
    Task<KnowledgeCorpusDiagnostics> GetCorpusDiagnosticsAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeSourceDiagnostics>> GetSourcesAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeChunkDiagnostics>> GetChunksAsync(string corpusId, string tenantId, KnowledgeChunkDiagnosticsQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnowledgeAskTraceDiagnostics>> GetAskTracesAsync(string corpusId, string tenantId, KnowledgeAskTraceDiagnosticsQuery query, CancellationToken cancellationToken);
    Task<KnowledgeAskDebugResult> DebugAskAsync(KnowledgeAskRequest request, CancellationToken cancellationToken);
    Task<KnowledgeIndexDiagnostics> GetIndexDiagnosticsAsync(string corpusId, string tenantId, CancellationToken cancellationToken);
}

public sealed record KnowledgeCorpusDiagnostics
{
    public required string CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Name { get; init; }
    public bool IsActive { get; init; }
    public int SourceCount { get; init; }
    public int DocumentCount { get; init; }
    public int ChunkCount { get; init; }
    public int QuestionCount { get; init; }
    public int AskTraceCount { get; init; }
    public DateTimeOffset? LastImportedAt { get; init; }
    public DateTimeOffset? LastAskedAt { get; init; }
}

public sealed record KnowledgeSourceDiagnostics
{
    public required string SourceId { get; init; }
    public required string CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Name { get; init; }
    public required string SourceType { get; init; }
    public string? OriginalLocation { get; init; }
    public string? Version { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
}

public sealed record KnowledgeChunkDiagnostics
{
    public required string ChunkId { get; init; }
    public required string SourceId { get; init; }
    public required string DocumentId { get; init; }
    public required string Title { get; init; }
    public string? HeadingPath { get; init; }
    public int Order { get; init; }
    public int TextLength { get; init; }
    public required string TextPreview { get; init; }
    public required string Text { get; init; }
}

public sealed record KnowledgeAskTraceDiagnostics
{
    public required string TraceId { get; init; }
    public required string CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Question { get; init; }
    public bool IsAnswered { get; init; }
    public string? MissingInformation { get; init; }
    public required IReadOnlyList<string> RetrievedChunkIds { get; init; }
    public required string GeneratedAnswer { get; init; }
    public required string CitationsJson { get; init; }
    public string? TraceKind { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed record KnowledgeChunkDiagnosticsQuery
{
    public string? SourceId { get; init; }
    public string? DocumentId { get; init; }
    public string? HeadingContains { get; init; }
    public string? TextContains { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record KnowledgeAskTraceDiagnosticsQuery
{
    public bool? IsAnswered { get; init; }
    public string? TraceKind { get; init; }
    public string? QuestionContains { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Limit { get; init; } = 100;
}

public sealed record KnowledgeAskDebugResult
{
    public required string CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Question { get; init; }
    public required IReadOnlyList<KnowledgeHit> RetrievedHits { get; init; }
    public required IReadOnlyList<string> ContextChunkIds { get; init; }
    public required KnowledgeAnswer Answer { get; init; }
    public required KnowledgeFailureStage FailureStage { get; init; }
    public string? FailureReason { get; init; }
}

public enum KnowledgeFailureStage
{
    None,
    NoImportedChunks,
    Retrieval,
    ContextPacking,
    Generation,
    CitationValidation,
    InsufficientContext
}

public sealed record KnowledgeIndexDiagnostics
{
    public required string CorpusId { get; init; }
    public required string TenantId { get; init; }
    public int StoredChunkCount { get; init; }
    public int SearchHitCountForProbe { get; init; }
    public DateTimeOffset? LastIndexedAt { get; init; }
    public bool LooksIndexed { get; init; }
}
