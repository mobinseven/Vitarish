using Vitarish.Entities.Server.Core;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant within a client application that is managed by an owner.
/// </summary>
public class Tenant : Entity
{
    /// <summary>Owner identifier.</summary>
    public Guid OwnerId
    {
        get;
        set => SetField(ref field, value, nameof(OwnerId));
    }

    /// <summary>Tenant name.</summary>
    public required string Name
    {
        get;
        set => SetField(ref field, value, nameof(Name));
    } = string.Empty;

    /// <summary>Tenant logo URL.</summary>
    public string? LogoUrl
    {
        get;
        set => SetField(ref field, value, nameof(LogoUrl));
    }

    /// <summary>Tenant description.</summary>
    public string? Description
    {
        get;
        set => SetField(ref field, value, nameof(Description));
    }

    public required string AppId
    {
        get;
        set => SetField(ref field, value, nameof(AppId));
    } = string.Empty;

    public Dictionary<string, object?> Metadata
    {
        get;
        set => SetField(ref field, value, nameof(Metadata));
    } = [];

    /// <summary>Collection of tenant indicators (hostnames/subdomains).</summary>
    public ICollection<TenantIndicator> Indicators
    {
        get;
        set => SetField(ref field, value, nameof(Indicators));
    } = [];
}
