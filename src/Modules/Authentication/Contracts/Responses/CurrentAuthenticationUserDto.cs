namespace Vitarish.Authentication.Contracts.Responses;

public sealed record CurrentAuthenticationUserDto(
    string UserId,
    string? DisplayName,
    string? GivenName,
    string? FamilyName,
    string? Email,
    string? PhoneNumber);
