using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Abstractions;

public sealed record KnowledgeImportRequest
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required Stream Content { get; init; }
    public required string Format { get; init; }
    public string? SourceName { get; init; }
}

public sealed record KnowledgeImportResult
{
    public required Guid CorpusId { get; init; }
    public required IReadOnlyList<KnowledgeSource> Sources { get; init; }
    public required IReadOnlyList<KnowledgeDocument> Documents { get; init; }
    public required IReadOnlyList<KnowledgeChunk> Chunks { get; init; }
    public IReadOnlyList<KnowledgeQuestion> Questions { get; init; } = [];
}

public sealed record KnowledgeIndexResult(Guid CorpusId, string TenantId, int IndexedChunkCount, string Scope, Guid? SourceId);

public sealed record KnowledgeQuery
{
    public required Guid CorpusId { get; init; }
    public required string Question { get; init; }
    public required string TenantId { get; init; }
    public int TopK { get; init; } = 8;
    public IReadOnlyDictionary<string, string>? Filters { get; init; }
}

public sealed record KnowledgeHit
{
    public required Guid ChunkId { get; init; }
    public required Guid SourceId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string Text { get; init; }
    public string? Title { get; init; }
    public double Score { get; init; }
}

public sealed record KnowledgeContext(string Question, string Text, IReadOnlyList<Guid> ChunkIds);

public sealed record KnowledgeAskRequest
{
    public required Guid CorpusId { get; init; }
    public required string Question { get; init; }
    public required string TenantId { get; init; }
    public int TopK { get; init; } = 8;
    public bool SaveTrace { get; init; } = true;
    public string? TraceKind { get; init; }
}

public sealed record KnowledgeAnswer
{
    public required string Answer { get; init; }
    public required bool IsAnswered { get; init; }
    public required IReadOnlyList<KnowledgeCitation> Citations { get; init; }
    public string? MissingInformation { get; init; }
    public IReadOnlyList<KnowledgeHit> RetrievedContext { get; init; } = [];
}

public sealed record KnowledgeCitation
{
    public required Guid ChunkId { get; init; }
    public required Guid SourceId { get; init; }
    public required Guid DocumentId { get; init; }
    public string? Title { get; init; }
    public string? Quote { get; init; }
}

public sealed record KnowledgeEvaluationRequest(Guid CorpusId, string TenantId, int TopK = 10);
public sealed record KnowledgeEvaluationRun(Guid Id, Guid CorpusId, string TenantId, IReadOnlyList<KnowledgeEvaluationItem> Items, double RecallAt5, double RecallAt10, double AverageExpectedChunkRank);

public sealed record KnowledgeEvaluationItem
{
    public required Guid QuestionId { get; init; }
    public bool HasExpectedContext { get; init; }
    public bool ExpectedContextFoundInTop5 { get; init; }
    public bool ExpectedContextFoundInTop10 { get; init; }
    public bool WasAnswered { get; init; }
    public bool ShouldHaveAnswered { get; init; }
    public string? GeneratedAnswer { get; init; }
}

public sealed record KnowledgeAskTrace
{
    public required Guid Id { get; init; }
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Question { get; init; }
    public required IReadOnlyList<Guid> RetrievedChunkIds { get; init; }
    public required string GeneratedAnswer { get; init; }
    public string? MissingInformation { get; init; }
    public required string CitationsJson { get; init; }
    public string? TraceKind { get; init; }
    public bool IsAnswered { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
