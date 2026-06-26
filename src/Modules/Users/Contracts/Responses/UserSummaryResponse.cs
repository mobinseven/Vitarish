namespace Vitarish.Users.Contracts.Responses;

public sealed record UserSummaryResponse(string Id, string? UserName, string? FirstName, string? LastName, string? Email, string? PhoneNumber, string? IdentificationCode, DateOnly? DateOfBirth, string? BirthCountryCode, string? BirthCity, string? FullName);
