using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ImportKnowledgeSourceFromFileInput(Guid? CorpusId, Guid? FileInformationId, string? SourceName, KnowledgeSourceInputFormat Format);
