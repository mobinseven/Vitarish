namespace Vitarish.Knowledge.Domain.Models;

public sealed record KnowledgeCorpus
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? TenantId { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public sealed record KnowledgeSource
{
    public required string Id { get; init; }
    public required string CorpusId { get; init; }
    public required string Name { get; init; }
    public required string SourceType { get; init; }
    public string? OriginalLocation { get; init; }
    public string? Version { get; init; }
    public string? Checksum { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
}

public sealed record KnowledgeDocument
{
    public required string Id { get; init; }
    public required string CorpusId { get; init; }
    public required string SourceId { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public string? Product { get; init; }
    public string? Version { get; init; }
    public string? Language { get; init; }
}

public sealed record KnowledgeChunk
{
    public required string Id { get; init; }
    public required string CorpusId { get; init; }
    public required string SourceId { get; init; }
    public required string DocumentId { get; init; }
    public required string Text { get; init; }
    public int Order { get; init; }
    public string? Title { get; init; }
    public string? HeadingPath { get; init; }
}

public sealed record KnowledgeChunkDraft
{
    public required string Text { get; init; }
    public int Order { get; init; }
    public string? Title { get; init; }
    public string? HeadingPath { get; init; }
}

public sealed record KnowledgeQuestion
{
    public required string Id { get; init; }
    public required string CorpusId { get; init; }
    public required string Question { get; init; }
    public string? ExpectedAnswer { get; init; }
    public IReadOnlyList<string> ExpectedChunkIds { get; init; } = [];
}
