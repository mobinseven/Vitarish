namespace Vitarish.Web.Clients.Contracts;

internal sealed record CreatePersonalAccessTokenRequest(
    string UserId,
    int? LifetimeDays,
    IEnumerable<string>? Scopes,
    bool IsReferenceToken = false);

internal sealed record PersonalAccessTokenResponse(string Token);
