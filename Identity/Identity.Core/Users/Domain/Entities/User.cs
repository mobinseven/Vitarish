using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Text.Json;
using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Identity.Core.Users.Domain.Entities;

/// <summary>
/// Represents a user in the identity system.
/// </summary>
public class User : Entity<string>
{
    private Dictionary<string, string>? _profilePreferencesCache;
    private string? _profileJsonCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// Required for EF Core.
    /// </summary>
    private User() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    public User(string id, string userName, string? email = null, string? phoneNumber = null, string? firstName = null, string? lastName = null)
    {
        Id = id;
        UserName = userName;
        Email = email;
        PhoneNumber = phoneNumber;
        FirstName = firstName;
        LastName = lastName;
    }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    [MaxLength(256)]
    public string UserName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    [MaxLength(64)]
    public string? FirstName { get; private set; }

    /// <summary>
    /// Gets or sets the last name.
    /// </summary>
    [MaxLength(64)]
    public string? LastName { get; private set; }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [MaxLength(256)]
    public string? Email { get; private set; }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    [MaxLength(32)]
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// Gets or sets the profile JSON.
    /// </summary>
    public string? ProfileJson { get; private set; }

    /// <summary>
    /// Gets or sets the profile preferences.
    /// </summary>
    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get
        {
            if (string.IsNullOrEmpty(ProfileJson))
            {
                _profilePreferencesCache = [];
                _profileJsonCache = ProfileJson;
                return _profilePreferencesCache;
            }

            if (_profilePreferencesCache == null || _profileJsonCache != ProfileJson)
            {
                try
                {
                    _profilePreferencesCache = JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson) ?? [];
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"Failed to deserialize profile preferences for user '{Id}': {ex}");
                    _profilePreferencesCache = [];
                }

                _profileJsonCache = ProfileJson;
            }

            return _profilePreferencesCache;
        }
        private set
        {
            if (value is null || value.Count == 0)
            {
                ProfileJson = null;
                _profilePreferencesCache = [];
            }
            else
            {
                ProfileJson = JsonSerializer.Serialize(value);
                _profilePreferencesCache = new Dictionary<string, string>(value);
            }

            _profileJsonCache = ProfileJson;
        }
    }

    /// <summary>
    /// Updates the user's name.
    /// </summary>
    public void UpdateName(string? firstName, string? lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        MarkAsUpdated();
    }

    /// <summary>
    /// Updates the user's email.
    /// </summary>
    public void UpdateEmail(string? email)
    {
        Email = email;
        MarkAsUpdated();
    }

    /// <summary>
    /// Updates the user's phone number.
    /// </summary>
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
        MarkAsUpdated();
    }

    /// <summary>
    /// Updates the user's profile preferences.
    /// </summary>
    public void UpdateProfilePreferences(Dictionary<string, string> preferences)
    {
        ProfilePreferences = preferences;
        MarkAsUpdated();
    }
}
