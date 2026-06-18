namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerCorpusOption(string Label, string Details)
{
    public Guid CorpusId { get; init; }
}
