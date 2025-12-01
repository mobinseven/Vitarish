using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Identity.Core.Parties.Domain.Entities;

/// <summary>
/// Represents an individual customer who can oversee multiple tenants.
/// </summary>
public class Owner : Entity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Owner"/> class.
    /// Required for EF Core.
    /// </summary>
    private Owner() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Owner"/> class.
    /// </summary>
    public Owner(Guid id, string name, string? logoUrl = null, string? description = null, string? createdBy = null)
    {
        Id = id;
        Name = name;
        LogoUrl = logoUrl;
        Description = description;
        CreatedBy = createdBy;
    }

    /// <summary>Owner name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Owner logo URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Owner description.</summary>
    public string? Description { get; private set; }

    /// <summary>User who created the owner.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>User who last modified the owner.</summary>
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Updates the owner details.
    /// </summary>
    public void Update(string name, string? logoUrl, string? description, string? modifiedBy)
    {
        Name = name;
        LogoUrl = logoUrl;
        Description = description;
        ModifiedBy = modifiedBy;
        MarkAsUpdated();
    }
}
