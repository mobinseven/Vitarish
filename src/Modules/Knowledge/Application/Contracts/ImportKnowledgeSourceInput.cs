namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ImportKnowledgeSourceInput(Guid? CorpusId, string? SourceName, string FormatName, string Content);
