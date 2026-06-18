namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerReadModel(IReadOnlyList<KnowledgeOwnerCorpusOption> Corpora, KnowledgeOwnerCorpusOption? SelectedCorpus, IReadOnlyList<KnowledgeOwnerSourceOption> Sources)
{
    public static KnowledgeOwnerReadModel Empty { get; } = new([], null, []);
}
