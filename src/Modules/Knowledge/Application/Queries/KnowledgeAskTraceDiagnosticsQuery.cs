namespace Vitarish.Knowledge.Application.Queries;

public sealed record KnowledgeAskTraceDiagnosticsQuery
{
    public bool? IsAnswered { get; init; }
    public string? TraceKind { get; init; }
    public string? QuestionContains { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int Limit { get; init; } = 100;
}
