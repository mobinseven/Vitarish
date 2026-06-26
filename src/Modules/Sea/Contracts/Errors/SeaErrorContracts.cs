namespace Vitarish.Sea.Contracts.Errors;

public enum SeaErrorCode
{
    ValidationFailure,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Timeout,
    Cancellation,
    TransportFailure,
    DeserializationFailure,
    UnexpectedFailure
}

public sealed record SeaError(SeaErrorCode Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
