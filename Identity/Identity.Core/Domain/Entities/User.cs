using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Vitarish.Identity.Core.Domain.Entities;

/// <summary>
/// Represents a user in the identity system.
/// Extends from IdentityUser for ASP.NET Core Identity integration.
/// </summary>
public class User
{
    /// <summary>Primary identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>User's username.</summary>
    public required string UserName { get; set; }

    /// <summary>User's email.</summary>
    public string? Email { get; set; }

    /// <summary>Whether the email is confirmed.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>User's first name.</summary>
    [MaxLength(64)]
    public string? FirstName { get; set; }

    /// <summary>User's last name.</summary>
    [MaxLength(64)]
    public string? LastName { get; set; }

    /// <summary>JSON representation of profile preferences.</summary>
    public string? ProfileJson { get; set; }

    /// <summary>Profile preferences as a dictionary.</summary>
    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson)!;
        set => ProfileJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value);
    }

    /// <summary>Date when the user was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date when the user was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }
}
