using Vitarish.Base.Domain.Primitives;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Core.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant within a client application that is managed by an owner.
/// </summary>
public class Tenant : Entity<Guid>
{
    private readonly List<TenantIndicator> _indicators = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenant"/> class.
    /// Required for EF Core.
    /// </summary>
    private Tenant() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Tenant"/> class.
    /// </summary>
    public Tenant(Guid id, Guid ownerId, string name, string clientId, string? logoUrl = null, string? description = null, string? createdBy = null)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        ClientId = clientId;
        LogoUrl = logoUrl;
        Description = description;
        CreatedBy = createdBy;
    }

    /// <summary>Owner identifier.</summary>
    public Guid OwnerId { get; private set; }

    /// <summary>Owning customer.</summary>
    public Owner? Owner { get; private set; }

    /// <summary>Tenant name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tenant logo URL.</summary>
    public string? LogoUrl { get; private set; }

    /// <summary>Tenant description.</summary>
    public string? Description { get; private set; }

    /// <summary>Client application identifier.</summary>
    public string ClientId { get; private set; } = string.Empty;

    /// <summary>User who created the tenant.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>User who last modified the tenant.</summary>
    public string? ModifiedBy { get; private set; }

    /// <summary>Collection of tenant indicators (hostnames/subdomains).</summary>
    public IReadOnlyCollection<TenantIndicator> Indicators => _indicators.AsReadOnly();

    /// <summary>
    /// Updates tenant details.
    /// </summary>
    public void Update(string name, string clientId, string? logoUrl, string? description, string? modifiedBy)
    {
        Name = name;
        ClientId = clientId;
        LogoUrl = logoUrl;
        Description = description;
        ModifiedBy = modifiedBy;
        MarkAsUpdated();
    }

    /// <summary>
    /// Adds a new tenant indicator.
    /// </summary>
    public TenantIndicator AddIndicator(IndicatorMethod method, string value, bool isPrimary, IndicatorStatus status, VerificationInfo? verification)
    {
        TenantIndicator indicator = new(Guid.NewGuid(), Id, method, value, status, isPrimary, verification);
        _indicators.Add(indicator);
        return indicator;
    }
}
