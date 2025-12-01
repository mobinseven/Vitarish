using System.Security.Claims;

namespace Vitarish.Identity.Contracts.Users;

/// <summary>
/// Data transfer object for a claim.
/// </summary>
public class ClaimDto
{
    public ClaimDto() { }
    public ClaimDto(string? type, string? value)
    {
        Type = type;
        Value = value;
    }
    public string? Type { get; init; }
    public string? Value { get; init; }
    public Claim ToClaim() => new(Type ?? string.Empty, Value ?? string.Empty);
}
