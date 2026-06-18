using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ReplaceKnowledgeSourceInput(Guid? CorpusId, Guid? SourceId, string? SourceName, KnowledgeSourceInputFormat Format, string Content)
{
    public ImportKnowledgeSourceInput ToImportInput() => new(CorpusId, SourceName, Format, Content);
}
