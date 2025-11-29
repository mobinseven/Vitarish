namespace Vitarish.Identity.Core.Domain.Entities;

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

/// <summary>
/// Represents a user in the identity system.
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
    /// Gets or sets the serialized JSON profile data.
    /// </summary>
    [PersonalData]
    public string? ProfileJson { get; set; }

    /// <summary>
    /// Gets or sets profile preferences as a dictionary.
    /// </summary>
    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson)!;
        set => ProfileJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value);
    }

    /// <summary>
    /// Gets the user's full name.
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
