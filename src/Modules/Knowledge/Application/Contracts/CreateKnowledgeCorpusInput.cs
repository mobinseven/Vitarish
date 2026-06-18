namespace Vitarish.Knowledge.Application.Contracts;

public sealed record CreateKnowledgeCorpusInput(string Name, string? Description, bool IsActive);
