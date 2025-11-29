namespace Vorn.Aaas.Client.Models;

using System.Collections.Generic;

internal sealed record CreatePersonalAccessTokenRequest(
    string UserId,
    int? LifetimeDays,
    IEnumerable<string>? Scopes,
    bool IsReferenceToken = false);

internal sealed record PersonalAccessTokenResponse(string Token);
