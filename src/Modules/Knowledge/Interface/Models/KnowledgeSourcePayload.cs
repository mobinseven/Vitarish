using Vitarish.Knowledge.Application.Enums;

namespace Vitarish.Knowledge.Interface.Models;

public sealed record KnowledgeSourcePayload(string? SourceName, KnowledgeSourceInputFormat Format, string Content = "");
