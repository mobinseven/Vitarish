using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace Vitarish.Identity.Core.Domain.Entities;

/// <summary>
/// Represents an identity user in the system.
/// Extends the ASP.NET Core Identity User with additional profile information.
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [MaxLength(64)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [MaxLength(64)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Gets or sets the JSON representation of user profile preferences.
    /// </summary>
    [PersonalData]
    public string? ProfileJson { get; set; }

    /// <summary>
    /// Gets or sets user profile preferences.
    /// Serialized to/from ProfileJson for storage.
    /// </summary>
    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson)
            ? []
            : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson) ?? [];
        set => ProfileJson = value is null || value.Count == 0
            ? null
            : JsonSerializer.Serialize(value);
    }
}
