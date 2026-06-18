using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Application.Contracts;

public sealed record ImportKnowledgeSourceInput(Guid? CorpusId, string? SourceName, KnowledgeSourceInputFormat Format, string Content);
