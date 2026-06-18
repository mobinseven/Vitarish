namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeCorpusDiagnostics
{
    public required Guid CorpusId { get; init; }
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
