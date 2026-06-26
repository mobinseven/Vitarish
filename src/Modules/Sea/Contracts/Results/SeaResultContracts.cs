using Vitarish.Sea.Contracts.Errors;

namespace Vitarish.Sea.Contracts.Results;

public sealed record SeaResult<T>(T? Value, SeaError? Error = null)
{
    public bool Succeeded => Error is null;
    public static SeaResult<T> Success(T value) => new(value);
    public static SeaResult<T> Failure(SeaError error) => new(default, error);
}

public sealed record SeaCommandResult(bool Succeeded, Guid? EntityId = null, SeaError? Error = null);
public sealed record SeaQueryResult<T>(T? Value, SeaError? Error = null)
{
    public bool Succeeded => Error is null;
}
