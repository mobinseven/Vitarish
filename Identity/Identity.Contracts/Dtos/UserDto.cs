namespace Vitarish.Identity.Contracts.Dtos;

/// <summary>
/// Data transfer object for user data.
/// </summary>
public class UserDto
{
    /// <summary>
    /// Gets or sets the user's unique identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the user's phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets or sets the user's access claim for a specific client.
    /// </summary>
    public ClaimDto? AccessClaim { get; set; }

    /// <summary>
    /// Gets or sets the user's profile preferences.
    /// </summary>
    public Dictionary<string, string>? ProfilePreferences { get; set; }
}
