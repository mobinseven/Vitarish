namespace Vorn.Aaas.Users.Presentation.Api;

using System.Collections.Generic;

public sealed record CreatePersonalAccessTokenRequest(
    string UserId,
    int? LifetimeDays = null,
    IEnumerable<string>? Scopes = null,
    bool IsReferenceToken = false);

public sealed record PersonalAccessTokenResponse(string Token);
