namespace Vitarish.Clients.Interface.Identity.Contracts;

internal sealed record CreatePersonalAccessTokenRequest(
    string UserId,
    int? LifetimeDays,
    IEnumerable<string>? Scopes,
    bool IsReferenceToken = false);
