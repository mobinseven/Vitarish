using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ReplaceKnowledgeSourceFromFileInput(Guid? CorpusId, Guid? SourceId, Guid? FileInformationId, string? SourceName, KnowledgeSourceInputFormat Format);
