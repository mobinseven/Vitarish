namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ReplaceKnowledgeSourceInput(Guid? CorpusId, Guid? SourceId, string? SourceName, string FormatName, string Content)
{
    public ImportKnowledgeSourceInput ToImportInput() => new(CorpusId, SourceName, FormatName, Content);
}
