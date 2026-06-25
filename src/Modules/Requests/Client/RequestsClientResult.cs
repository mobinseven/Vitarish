namespace Vitarish.Requests.Client;

public enum RequestsClientFailureKind
{
    None,
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

public sealed record RequestsClientFailure(
    RequestsClientFailureKind Kind,
    string Message,
    int? StatusCode = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record RequestsClientResult<T>(T? Value, RequestsClientFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static RequestsClientResult<T> Success(T value) => new(value, null);

    public static RequestsClientResult<T> Failed(RequestsClientFailure failure) => new(default, failure);
}
