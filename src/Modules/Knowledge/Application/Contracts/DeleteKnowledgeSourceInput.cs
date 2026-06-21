namespace Vitarish.Knowledge.Application.Contracts;

public sealed record DeleteKnowledgeSourceInput(Guid? CorpusId, Guid? SourceId);
