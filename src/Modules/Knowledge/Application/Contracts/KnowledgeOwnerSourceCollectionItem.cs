namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerSourceCollectionItem(
    Guid SourceId,
    string SourceName,
    string SourceType,
    DateTimeOffset ImportedAt,
    string? OriginalLocation,
    IReadOnlyList<KnowledgeOwnerSourceFileCollectionItem> Files);
