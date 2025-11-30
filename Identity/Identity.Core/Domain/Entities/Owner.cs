namespace Vitarish.Identity.Core.Domain.Entities;

/// <summary>
/// Represents an individual customer who can oversee multiple tenants.
/// </summary>
public class Owner
{
    /// <summary>Primary identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner name.</summary>
    public required string Name { get; set; }

    /// <summary>Owner logo URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Owner description.</summary>
    public string? Description { get; set; }

    /// <summary>Date when the owner was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who created the owner.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Date when the owner was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>User who last modified the owner.</summary>
    public string? ModifiedBy { get; set; }
}
