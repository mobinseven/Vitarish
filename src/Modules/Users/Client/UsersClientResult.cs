namespace Vitarish.Users.Client;

public sealed record UsersClientResult<T>(T? Value, UsersClientFailure? Failure)
{
    public bool IsSuccess => Failure is null;
    public static UsersClientResult<T> Success(T value) => new(value, null);
    public static UsersClientResult<T> Failed(UsersClientFailure failure) => new(default, failure);
}

public sealed record UsersClientFailure(UsersClientFailureKind Kind, string Message, int? StatusCode = null);

public enum UsersClientFailureKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Timeout,
    Canceled,
    Transport,
    Deserialization,
    UnexpectedServerFailure
}
