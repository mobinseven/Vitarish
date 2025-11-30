namespace Vorn.Aaas.Users.Presentation.Contracts;

public class UserDto
{
    public string Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public ClaimDto? AccessClaim { get; set; }
    public Dictionary<string, string>? ProfilePreferences { get; set; }
}
