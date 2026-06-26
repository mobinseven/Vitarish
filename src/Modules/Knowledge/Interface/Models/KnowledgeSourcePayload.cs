using Vitarish.Knowledge.Contracts.Enums;

namespace Vitarish.Knowledge.Interface.Models;

public sealed record KnowledgeSourcePayload(string? SourceName, KnowledgeSourceInputFormat Format, Guid FileId);
