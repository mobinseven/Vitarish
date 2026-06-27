using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Knowledge.Domain.Models;

public sealed record KnowledgeCorpus : EntityReadDto
{
    public required string Name { get; init; }
    public required string TenantId { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
}

public sealed record KnowledgeSource : EntityReadDto
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required string Name { get; init; }
    public required string SourceType { get; init; }
    public string? OriginalLocation { get; init; }
    public string? Version { get; init; }
    public string? Checksum { get; init; }
    public DateTimeOffset ImportedAt { get; init; }
}

public sealed record KnowledgeDocument : EntityReadDto
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required Guid SourceId { get; init; }
    public required string Title { get; init; }
    public required string Text { get; init; }
    public string? Product { get; init; }
    public string? Version { get; init; }
    public string? Language { get; init; }
}

public sealed record KnowledgeChunk : EntityReadDto
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public required Guid SourceId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string Text { get; init; }
    public int Order { get; init; }
    public string? Title { get; init; }
    public string? HeadingPath { get; init; }
}

public sealed record KnowledgeChunkDraft : EntityReadDto
{
    public required string Text { get; init; }
    public int Order { get; init; }
    public string? Title { get; init; }
    public string? HeadingPath { get; init; }
}

public sealed record KnowledgeQuestion : EntityReadDto
{
    public required Guid CorpusId { get; init; }
    public required string Question { get; init; }
    public string? ExpectedAnswer { get; init; }
    public IReadOnlyList<Guid> ExpectedChunkIds { get; init; } = [];
}

public sealed record KnowledgeIndexRun : EntityReadDto
{
    public required Guid CorpusId { get; init; }
    public required string TenantId { get; init; }
    public int IndexedChunkCount { get; init; }
    public string Scope { get; init; } = "corpus";
    public Guid? SourceId { get; init; }
}
