namespace Vitarish.Knowledge.Application.Abstractions;

public sealed record KnowledgeIndexDiagnostics
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public int StoredChunkCount { get; init; }
    public int SearchHitCountForProbe { get; init; }
    public DateTimeOffset? LastIndexedAt { get; init; }
    public bool LooksIndexed { get; init; }
}
