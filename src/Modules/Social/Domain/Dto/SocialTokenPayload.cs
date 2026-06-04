namespace Vitarish.Social.Domain.Dto;

public record SocialTokenPayload(
    string AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    IReadOnlyList<string> Scopes);
