using System.Security.Claims;

namespace Vitarish.Users.Domain.Models;

public class ClaimDto
{
    public ClaimDto() { }
    public ClaimDto(string? type, string? value)
    {
        Type = type ?? string.Empty;
        Value = value ?? string.Empty;
    }
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Claim ToClaim() => new(Type, Value);
}
