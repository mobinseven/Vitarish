using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Vitarish.Users.Domain.Entities;

public class User : IdentityUser
{
    [MaxLength(64)]
    public string? FirstName { get; set; }
    [MaxLength(64)]
    public string? LastName { get; set; }

    [PersonalData]
    [MaxLength(64)]
    public string? IdentificationCode { get; set; }

    [PersonalData]
    public DateOnly? DateOfBirth { get; set; }

    [PersonalData]
    [MaxLength(8)]
    public string? BirthCountryCode { get; set; }

    [PersonalData]
    [MaxLength(128)]
    public string? BirthCity { get; set; }

    [PersonalData]
    public string? ProfileJson { get; set; }

    /// <summary>
    /// Administrative account status. A disabled account cannot sign in and
    /// keeps its failed-attempt lockout state independent of this flag.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Terminal account deletion; the ID remains valid for clinical and audit attribution.</summary>
    public DateTime? DeletedAt { get; private set; }

    [NotMapped]
    public bool IsDeleted => DeletedAt.HasValue;

    public void EraseAccount(DateTime deletedAt, string securityStamp, string revision)
    {
        if (IsDeleted) return;
        if (deletedAt.Kind != DateTimeKind.Utc) throw new ArgumentException("Deletion time must be UTC.", nameof(deletedAt));
        DeletedAt = deletedAt;
        IsEnabled = false;
        FirstName = LastName = IdentificationCode = BirthCountryCode = BirthCity = ProfileJson = null;
        DateOfBirth = null;
        Email = NormalizedEmail = PhoneNumber = UserName = NormalizedUserName = null;
        EmailConfirmed = PhoneNumberConfirmed = TwoFactorEnabled = false;
        PasswordHash = null;
        PasswordChangedAt = null;
        if (PasswordHashFingerprint is { } fingerprint) System.Security.Cryptography.CryptographicOperations.ZeroMemory(fingerprint);
        PasswordHashFingerprint = null;
        LockoutEnd = null;
        LockoutEnabled = false;
        AccessFailedCount = 0;
        SecurityStamp = securityStamp;
        ConcurrencyStamp = revision;
    }

    /// <summary>UTC time of the last known successful password assignment.</summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>Credential-derived verifier for the stored password hash; never serialize it.</summary>
    [JsonIgnore]
    public byte[]? PasswordHashFingerprint { get; set; }

    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson)!;
        set => ProfileJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
