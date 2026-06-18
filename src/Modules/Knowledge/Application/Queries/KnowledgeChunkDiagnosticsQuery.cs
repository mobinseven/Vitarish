namespace Vitarish.Knowledge.Application.Queries;

public sealed record KnowledgeChunkDiagnosticsQuery
{
    public Guid? SourceId { get; init; }
    public Guid? DocumentId { get; init; }
    public string? HeadingContains { get; init; }
    public string? TextContains { get; init; }
    public int Limit { get; init; } = 100;
}
