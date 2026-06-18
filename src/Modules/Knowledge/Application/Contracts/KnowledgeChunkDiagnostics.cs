namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeChunkDiagnostics
{
    public required Guid ChunkId { get; init; }
    public required Guid SourceId { get; init; }
    public required Guid DocumentId { get; init; }
    public required string Title { get; init; }
    public string? HeadingPath { get; init; }
    public int Order { get; init; }
    public int TextLength { get; init; }
    public required string TextPreview { get; init; }
    public required string Text { get; init; }
}
