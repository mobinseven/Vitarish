using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
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

    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson)!;
        set => ProfileJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
