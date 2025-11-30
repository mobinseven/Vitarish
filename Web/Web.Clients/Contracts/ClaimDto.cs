namespace Vitarish.Web.Clients.Contracts;

/// <summary>
/// Data transfer object for a claim.
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// Creates a new ClaimDto instance.
    /// </summary>
    public ClaimDto() { }

    /// <summary>
    /// Creates a new ClaimDto with specified type and value.
    /// </summary>
    public ClaimDto(string? type, string? value)
    {
        Type = type;
        Value = value;
    }

    /// <summary>
    /// The claim type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// The claim value.
    /// </summary>
    public string? Value { get; set; }
}
