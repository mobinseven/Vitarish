namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeSourceDiagnostics
{
    public required Guid SourceId { get; init; }
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Name { get; init; }
    public required string SourceType { get; init; }
    public string? OriginalLocation { get; init; }
    public string? Version { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
}
