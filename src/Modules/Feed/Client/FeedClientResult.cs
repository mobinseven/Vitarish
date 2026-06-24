namespace Vitarish.Feed.Client;

public enum FeedClientFailureKind
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

public sealed record FeedClientFailure(
    FeedClientFailureKind Kind,
    string Message,
    int? StatusCode = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public sealed record FeedClientResult<T>(T? Value, FeedClientFailure? Failure)
{
    public bool IsSuccess => Failure is null;

    public static FeedClientResult<T> Success(T value) => new(value, null);

    public static FeedClientResult<T> Failed(FeedClientFailure failure) => new(default, failure);
}
