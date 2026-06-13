namespace Vitarish.Knowledge.Domain.Entities;

public sealed class KnowledgeCorpusEntity
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class KnowledgeSourceEntity
{
    public required string Id { get; set; }
    public required string CorpusId { get; set; }
    public string? TenantId { get; set; }
    public required string Name { get; set; }
    public required string SourceType { get; set; }
    public string? OriginalLocation { get; set; }
    public string? Version { get; set; }
    public string? Checksum { get; set; }
    public DateTimeOffset ImportedAt { get; set; }
}

public sealed class KnowledgeDocumentEntity
{
    public required string Id { get; set; }
    public required string CorpusId { get; set; }
    public string? TenantId { get; set; }
    public required string SourceId { get; set; }
    public required string Title { get; set; }
    public required string Text { get; set; }
    public string? Product { get; set; }
    public string? Version { get; set; }
    public string? Language { get; set; }
}

public sealed class KnowledgeChunkEntity
{
    public required string Id { get; set; }
    public required string CorpusId { get; set; }
    public string? TenantId { get; set; }
    public required string SourceId { get; set; }
    public required string DocumentId { get; set; }
    public required string Text { get; set; }
    public int Order { get; set; }
    public string? Title { get; set; }
    public string? HeadingPath { get; set; }
}

public sealed class KnowledgeQuestionEntity
{
    public required string Id { get; set; }
    public required string CorpusId { get; set; }
    public required string Question { get; set; }
    public string? ExpectedAnswer { get; set; }
    public required string ExpectedChunkIdsJson { get; set; }
}

public sealed class KnowledgeAskTraceEntity
{
    public required string Id { get; set; }
    public required string CorpusId { get; set; }
    public required string Question { get; set; }
    public required string RetrievedChunkIdsJson { get; set; }
    public required string GeneratedAnswer { get; set; }
    public bool IsAnswered { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
