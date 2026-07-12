namespace Vitarish.Platform.Contracts.Errors;

public sealed record PlatformError(PlatformErrorCode Code, string Message, int? StatusCode = null);
