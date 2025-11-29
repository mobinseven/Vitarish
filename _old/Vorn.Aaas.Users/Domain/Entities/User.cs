using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
namespace Vorn.Aaas.Users.Domain.Entities;
public class User : IdentityUser
{
    [MaxLength(64)]
    public string? FirstName { get; set; }
    [MaxLength(64)]
    public string? LastName { get; set; }

    [PersonalData]
    public string? ProfileJson { get; set; }

    [NotMapped]
    public Dictionary<string, string> ProfilePreferences
    {
        get => string.IsNullOrEmpty(ProfileJson) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(ProfileJson)!;
        set => ProfileJson = value is null || value.Count == 0 ? null : JsonSerializer.Serialize(value);
    }
}
