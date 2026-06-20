using Vitarish.Knowledge.Domain.Models;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeImportRequest
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required Stream Content { get; init; }
    public required string Format { get; init; }
    public string? SourceName { get; init; }
    public string? OriginalLocation { get; init; }
    public string? Checksum { get; init; }
    public string? Version { get; init; }
    public long? ContentLength { get; init; }
}

public sealed record KnowledgeImportFile(Stream Content, string DisplayName, string? Checksum, string OriginalLocation, string? ContentType, long? Size);

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

public sealed record KnowledgeEvaluationRun(Guid Id, Guid CorpusId, string TenantId, IReadOnlyList<KnowledgeEvaluationItem> Items, double RecallAt5, double RecallAt10, double AverageExpectedChunkRank);

public sealed record KnowledgeAnswerCorrectnessScore(bool? IsCorrect, string? Rationale);

public enum KnowledgeEvaluationItemStatus
{
    MissingGoldContext,
    GoldContextNotRetrieved,
    GoldContextRetrieved,
    AnsweredWithoutGoldContext,
    AbstainedWithGoldContext,
    UnsupportedAnswer,
    SupportedAnswerCandidate,
    CorrectAnswer
}

public sealed record KnowledgeEvaluationItem
{
    public required Guid RunId { get; init; }
    public required Guid QuestionId { get; init; }
    public bool HasExpectedAnswer { get; init; }
    public bool HasExpectedContext { get; init; }
    public bool ExpectedContextFoundInTop5 { get; init; }
    public bool ExpectedContextFoundInTop10 { get; init; }
    public bool ExpectedContextWasRetrieved { get; init; }
    /// <summary>True when the answerer produced an answer; this is not an answer correctness score.</summary>
    public bool WasAnswered { get; init; }
    /// <summary>True when the question has a gold-label expected answer.</summary>
    public bool ShouldHaveAnswered { get; init; }
    /// <summary>Null until a semantic answer correctness scorer is provided through an application abstraction.</summary>
    public bool? GeneratedAnswerMatchedExpectedAnswer { get; init; }
    public string? GeneratedAnswerCorrectnessRationale { get; init; }
    public string? GeneratedAnswer { get; init; }
    public KnowledgeEvaluationItemStatus Status { get; init; }
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
