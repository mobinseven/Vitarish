namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerCommandResult(bool Succeeded, string Message)
{
    public static KnowledgeOwnerCommandResult Success(string message) => new(true, message);
    public static KnowledgeOwnerCommandResult Failure(string message) => new(false, message);
}
