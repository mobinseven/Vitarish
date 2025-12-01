namespace Vitarish.Identity.Contracts.Users;

/// <summary>
/// Data transfer object for a user.
/// </summary>
public class UserDto
{
    public string Id { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string FullName => $"{FirstName} {LastName}";
    public ClaimDto? AccessClaim { get; init; }
    public Dictionary<string, string>? ProfilePreferences { get; init; }
}
