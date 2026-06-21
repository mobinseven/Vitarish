namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerCorpusCollectionItem(
    Guid CorpusId,
    string CorpusName,
    string? CorpusDescription,
    bool IsActive,
    IReadOnlyList<KnowledgeOwnerSourceCollectionItem> Sources);
