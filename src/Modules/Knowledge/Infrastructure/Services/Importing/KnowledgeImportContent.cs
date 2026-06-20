namespace Vitarish.Knowledge.Infrastructure.Services.Importing;

internal sealed record KnowledgeImportContent(IReadOnlyList<KnowledgeDocumentDraft> Documents, IReadOnlyList<KnowledgeQuestionDraft> Questions);

internal sealed record KnowledgeDocumentDraft
{
    public required string Title { get; init; }
    public required string Text { get; init; }
    public string? Product { get; init; }
    public string? Version { get; init; }
    public string? Language { get; init; }
}

internal sealed record KnowledgeQuestionDraft
{
    public required string Question { get; init; }
    public string? ExpectedAnswer { get; init; }
    public IReadOnlyList<string> ExpectedContextTexts { get; init; } = [];
    public IReadOnlyList<Guid> ExpectedChunkIds { get; init; } = [];
}
