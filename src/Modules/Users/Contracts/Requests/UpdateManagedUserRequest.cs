namespace Vitarish.Users.Contracts.Requests;

public sealed record UpdateManagedUserRequest(
    string? Email,
    string? PhoneNumber,
    string? FirstName,
    string? LastName,
    string? Revision);
