using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeAskDebugResult
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Question { get; init; }
    public required IReadOnlyList<KnowledgeHit> RetrievedHits { get; init; }
    public required IReadOnlyList<Guid> ContextChunkIds { get; init; }
    public required KnowledgeAnswer Answer { get; init; }
    public required KnowledgeFailureStage FailureStage { get; init; }
    public string? FailureReason { get; init; }
}
