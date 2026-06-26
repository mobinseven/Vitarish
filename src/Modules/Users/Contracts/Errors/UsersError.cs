namespace Vitarish.Users.Contracts.Errors;

public sealed record UsersError(UsersErrorCode Code, string Message, IReadOnlyDictionary<string, string[]>? ValidationErrors = null, int? StatusCode = null);
