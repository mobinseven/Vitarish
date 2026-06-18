namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeCheck
{
    public required bool Passed { get; init; }
    public required string Message { get; init; }
    public IReadOnlyDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();
}
