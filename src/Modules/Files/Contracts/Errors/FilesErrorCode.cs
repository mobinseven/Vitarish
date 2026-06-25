namespace Vitarish.Files.Contracts.Errors;

public enum FilesErrorCode
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
    Unexpected
}
