using System.Security.Claims;

namespace Vitarish.Identity.Contracts.Dtos;

/// <summary>
/// Data transfer object for claim data.
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// Initializes a new instance of <see cref="ClaimDto"/>.
    /// </summary>
    public ClaimDto() { }

    /// <summary>
    /// Initializes a new instance of <see cref="ClaimDto"/> with the specified type and value.
    /// </summary>
    /// <param name="type">The type of the claim.</param>
    /// <param name="value">The value of the claim.</param>
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
    /// Converts this DTO to a <see cref="Claim"/>.
    /// </summary>
    /// <returns>A <see cref="Claim"/> instance.</returns>
    public Claim ToClaim() => new(Type ?? string.Empty, Value ?? string.Empty);
}
