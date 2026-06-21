namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerCollectionReadModel(
    IReadOnlyList<KnowledgeOwnerCorpusCollectionItem> Corpora,
    Guid? SelectedCorpusId,
    Guid? SelectedSourceId)
{
    public static KnowledgeOwnerCollectionReadModel Empty { get; } = new([], null, null);
}
