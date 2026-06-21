namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerSourceFileCollectionItem(
    Guid FileId,
    string FileName,
    string? ContentType,
    long? Size,
    int ChunkCount,
    DateTimeOffset? IndexedAt);
