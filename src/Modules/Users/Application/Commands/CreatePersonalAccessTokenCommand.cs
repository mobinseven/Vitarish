namespace Vitarish.Users.Application.Commands;

public sealed record CreatePersonalAccessTokenCommand(
    string UserId,
    int? LifetimeDays = null,
    IEnumerable<string>? Scopes = null,
    bool IsReferenceToken = false);
