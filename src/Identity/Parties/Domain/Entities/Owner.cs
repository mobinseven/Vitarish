using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents an individual customer who can oversee multiple tenants.
/// </summary>
public class Owner : Entity
{
    private string _name = string.Empty;
    private string? _logoUrl;
    private string? _description;

    /// <summary>Owner name.</summary>
    public required string Name
    {
        get => _name;
        set => SetField(ref _name, value, nameof(Name));
    }

    /// <summary>Owner logo URL.</summary>
    public string? LogoUrl
    {
        get => _logoUrl;
        set => SetField(ref _logoUrl, value, nameof(LogoUrl));
    }

    /// <summary>Owner description.</summary>
    public string? Description
    {
        get => _description;
        set => SetField(ref _description, value, nameof(Description));
    }
}
