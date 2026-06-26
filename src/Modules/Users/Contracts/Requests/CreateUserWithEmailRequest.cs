namespace Vitarish.Users.Contracts.Requests;

public sealed record CreateUserWithEmailRequest(string Email, string? FirstName, string? LastName, string? IdentificationCode = null, DateOnly? DateOfBirth = null, string? BirthCountryCode = null, string? BirthCity = null);
