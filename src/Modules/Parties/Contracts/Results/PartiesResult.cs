using Vitarish.Parties.Contracts.Errors;
namespace Vitarish.Parties.Contracts.Results;
public sealed record PartiesResult<T>(T? Value, PartiesError? Error)
{
    public bool IsSuccess => Error is null;
    public static PartiesResult<T> Success(T value) => new(value, null);
    public static PartiesResult<T> Failed(PartiesError error) => new(default, error);
}
