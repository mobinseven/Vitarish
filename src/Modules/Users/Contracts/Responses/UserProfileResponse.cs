namespace Vitarish.Users.Contracts.Responses;

public sealed record UserProfileResponse(
    string UserId,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? Culture,
    string? Theme);
