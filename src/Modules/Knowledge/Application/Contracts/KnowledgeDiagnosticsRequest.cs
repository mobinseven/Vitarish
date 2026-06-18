namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeDiagnosticsRequest
{
    public required string TenantId { get; init; }
    public required Guid CorpusId { get; init; }
    public string? Question { get; init; }
    public int TopK { get; init; } = 8;
}
