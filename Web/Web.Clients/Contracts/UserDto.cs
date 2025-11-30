namespace Vitarish.Web.Clients.Contracts;

/// <summary>
/// Data transfer object for a user.
/// </summary>
public class UserDto
{
    /// <summary>
    /// User's unique identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// User's username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// User's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// User's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// User's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// User's full name (first + last).
    /// </summary>
    public string FullName => $"{FirstName} {LastName}";

    /// <summary>
    /// User's access claim.
    /// </summary>
    public ClaimDto? AccessClaim { get; set; }

    /// <summary>
    /// User's access permissions.
    /// </summary>
    public Access? Access { get; set; }

    /// <summary>
    /// User's profile preferences.
    /// </summary>
    public Dictionary<string, string>? ProfilePreferences { get; set; }
}
