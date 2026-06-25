namespace Vitarish.Files.Contracts.Errors;

public sealed record FilesError(FilesErrorCode Code, string Message, int? StatusCode = null);
