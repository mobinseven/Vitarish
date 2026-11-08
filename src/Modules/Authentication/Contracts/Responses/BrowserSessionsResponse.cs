namespace Vitarish.Authentication.Contracts.Responses;

public sealed record BrowserSessionDto(string SessionId, DateTime CreatedAt, DateTime LastActivityAt, bool IsCurrent);
public sealed record BrowserSessionsResponse(IReadOnlyList<BrowserSessionDto> Sessions);
