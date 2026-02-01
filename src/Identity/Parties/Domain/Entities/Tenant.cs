using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant within a client application that is managed by an owner.
/// </summary>
public class Tenant : Entity
{
    private Guid _ownerId;
    private string _name = string.Empty;
    private string? _logoUrl;
    private string? _description;
    private string _clientId = string.Empty;
    private Dictionary<string, object?> _metadata = [];
    private ICollection<TenantIndicator> _indicators = [];

    /// <summary>Owner identifier.</summary>
    public Guid OwnerId
    {
        get => _ownerId;
        set => SetField(ref _ownerId, value, nameof(OwnerId));
    }

    /// <summary>Tenant name.</summary>
    public required string Name
    {
        get => _name;
        set => SetField(ref _name, value, nameof(Name));
    }

    /// <summary>Tenant logo URL.</summary>
    public string? LogoUrl
    {
        get => _logoUrl;
        set => SetField(ref _logoUrl, value, nameof(LogoUrl));
    }

    /// <summary>Tenant description.</summary>
    public string? Description
    {
        get => _description;
        set => SetField(ref _description, value, nameof(Description));
    }

    /// <summary>Client application identifier.</summary>
    public required string ClientId
    {
        get => _clientId;
        set => SetField(ref _clientId, value, nameof(ClientId));
    }

    public Dictionary<string, object?> Metadata
    {
        get => _metadata;
        set => SetField(ref _metadata, value, nameof(Metadata));
    }

    /// <summary>Collection of tenant indicators (hostnames/subdomains).</summary>
    public ICollection<TenantIndicator> Indicators
    {
        get => _indicators;
        set => SetField(ref _indicators, value, nameof(Indicators));
    }
}
