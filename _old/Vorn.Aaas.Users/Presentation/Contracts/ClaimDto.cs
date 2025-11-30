using System.Security.Claims;

namespace Vorn.Aaas.Users.Presentation.Contracts;

public class ClaimDto
{
    public ClaimDto() { }
    public ClaimDto(string? type, string? value)
    {
        Type = type;
        Value = value;
    }
    public string? Type { get; set; }
    public string? Value { get; set; }
    public Claim ToClaim() => new(Type, Value);
}
