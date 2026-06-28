namespace Vitarish.Authentication.Contracts.Responses;

public sealed record CurrentAuthenticationUserDto(string UserId, string? DisplayName, string? Email, string? PhoneNumber);
