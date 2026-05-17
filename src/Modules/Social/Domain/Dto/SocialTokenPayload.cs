namespace Vitarish.Social.Domain.Dto;

public record SocialTokenPayload(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<string> Scopes);
