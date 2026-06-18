namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerSourceOption(string Label)
{
    public Guid SourceId { get; init; }
}
