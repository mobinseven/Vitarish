namespace Vitarish.Social.Domain.Dto;

public record SocialProfile(
    string ExternalId,
    string? Username,
    string? DisplayName,
    string RawSnapshot);
