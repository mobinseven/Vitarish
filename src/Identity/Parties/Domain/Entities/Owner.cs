using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents an individual customer who can oversee multiple tenants.
/// </summary>
public class Owner : Entity
{
    /// <summary>Owner name.</summary>
    public required string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;

    /// <summary>Owner logo URL.</summary>
    public string? LogoUrl
    {
        get;
        set => SetField(ref field, value, nameof(LogoUrl));
    }

    /// <summary>Owner description.</summary>
    public string? Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    }
}
