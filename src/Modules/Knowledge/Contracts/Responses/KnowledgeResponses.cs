using Vitarish.Knowledge.Contracts.Models;

namespace Vitarish.Knowledge.Contracts.Responses;

public sealed record CreateKnowledgeCorpusResponse(bool Succeeded, string Message, KnowledgeOwnerCorpusOption? Corpus);
public sealed record ImportKnowledgeSourceResponse(bool Succeeded, string Message, string? Summary);
public sealed record ReplaceKnowledgeSourceResponse(bool Succeeded, string Message, string? Summary);
public sealed record DeleteKnowledgeSourceResponse(bool Succeeded, string Message);
public sealed record IndexKnowledgeCorpusResponse(bool Succeeded, string Message);
public sealed record EvaluateKnowledgeCorpusResponse(bool Succeeded, string Message, string? Summary);
