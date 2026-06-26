using Vitarish.Knowledge.Contracts.Errors;

namespace Vitarish.Knowledge.Contracts.Results;

public sealed record KnowledgeResult<T>(T? Value, KnowledgeContractError? Error)
{
    public bool Succeeded => Error is null;
    public static KnowledgeResult<T> Success(T value) => new(value, null);
    public static KnowledgeResult<T> Failed(KnowledgeContractError error) => new(default, error);
}

public sealed record KnowledgeCommandResult(bool Succeeded, string Message, string? Summary = null, KnowledgeContractError? Error = null);
