namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerCreateCorpusResult(bool Succeeded, string Message, KnowledgeOwnerCorpusOption? Corpus)
{
    public static KnowledgeOwnerCreateCorpusResult Success(string message, KnowledgeOwnerCorpusOption corpus) => new(true, message, corpus);
    public static KnowledgeOwnerCreateCorpusResult Failure(string message) => new(false, message, null);
}
