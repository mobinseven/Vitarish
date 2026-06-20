namespace Vitarish.Knowledge.Application.Contracts;

public enum KnowledgeRetrievalMode
{
    Balanced,
    Broad
}

public sealed record KnowledgeRetrievalPlan
{
    public required string OriginalQuestion { get; init; }
    public required string CleanedQuery { get; init; }
    public required IReadOnlyList<string> SearchQueries { get; init; }
    public IReadOnlyList<string> PreservedTerms { get; init; } = [];
    public IReadOnlyList<string> ExpandedTerms { get; init; } = [];
    public IReadOnlyList<string> DroppedTerms { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
