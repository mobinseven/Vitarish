namespace Vitarish.Users.Contracts.Requests;

public sealed record CheckUserEmailInUseQueryParameters(string Email, string? ExcludeUserId = null);
