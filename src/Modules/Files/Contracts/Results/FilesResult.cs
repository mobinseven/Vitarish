using Vitarish.Files.Contracts.Errors;

namespace Vitarish.Files.Contracts.Results;

public sealed record FilesResult<T>(T? Value, FilesError? Error)
{
    public bool Succeeded => Error is null;
    public static FilesResult<T> Success(T value) => new(value, null);
    public static FilesResult<T> Failed(FilesError error) => new(default, error);
}
