namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeAskTraceDiagnostics
{
    public required Guid TraceId { get; init; }
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Question { get; init; }
    public bool IsAnswered { get; init; }
    public string? MissingInformation { get; init; }
    public required IReadOnlyList<Guid> RetrievedChunkIds { get; init; }
    public string? RetrievedChunkIdsParseError { get; init; }
    public required string GeneratedAnswer { get; init; }
    public required IReadOnlyList<KnowledgeCitation> Citations { get; init; }
    public string? CitationsParseError { get; init; }
    public string? TraceKind { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
