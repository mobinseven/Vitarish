namespace Vitarish.Clients.Interface.Identity.Contracts;

public class UserDto
{
    public required string Id { get; set; }
    public string? UserName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string FullName => $"{FirstName} {LastName}";
    public ClaimDto? AccessClaim { get; set; }
    public AccessOld? Access { get; set; }
    public Dictionary<string, string>? ProfilePreferences { get; set; }
}
