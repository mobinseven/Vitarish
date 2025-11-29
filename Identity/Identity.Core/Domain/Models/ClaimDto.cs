namespace Vitarish.Identity.Core.Domain.Models;

using System.Security.Claims;

/// <summary>
/// Data transfer object for claims.
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// Initializes a new instance of ClaimDto.
    /// </summary>
    public ClaimDto() { }

    /// <summary>
    /// Initializes a new instance of ClaimDto with type and value.
    /// </summary>
    /// <param name="type">The claim type.</param>
    /// <param name="value">The claim value.</param>
    public ClaimDto(string? type, string? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the claim type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the claim value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Converts this DTO to a Claim object.
    /// </summary>
    /// <returns>A Claim object.</returns>
    public Claim ToClaim() => new(Type ?? string.Empty, Value ?? string.Empty);
}
