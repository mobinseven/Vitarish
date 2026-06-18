namespace Vitarish.Knowledge.Application.Contracts;

public sealed record KnowledgeOwnerImportCommandResult(bool Succeeded, string Message, string? Summary)
{
    public static KnowledgeOwnerImportCommandResult Success(string message, string summary) => new(true, message, summary);
    public static KnowledgeOwnerImportCommandResult Failure(string message) => new(false, message, null);
}
