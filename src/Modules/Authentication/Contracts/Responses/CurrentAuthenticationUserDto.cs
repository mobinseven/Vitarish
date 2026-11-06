namespace Vitarish.Authentication.Contracts.Responses;

using Vitarish.Authentication.Contracts.Enums;

public sealed record CurrentAuthenticationUserDto(
    string UserId,
    string? DisplayName,
    string? GivenName,
    string? FamilyName,
    string? Email,
    string? PhoneNumber,
    bool HasPassword = false,
    bool TwoFactorEnabled = false,
    TwoStepSignInChannelDto? TwoFactorChannel = null);
