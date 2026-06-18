namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerEvaluationCommandResult(bool Succeeded, string Message, string? Summary)
{
    public static KnowledgeOwnerEvaluationCommandResult Success(string message, string summary) => new(true, message, summary);
    public static KnowledgeOwnerEvaluationCommandResult Failure(string message) => new(false, message, null);
}
