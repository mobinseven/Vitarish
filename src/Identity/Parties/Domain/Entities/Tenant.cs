using System.ComponentModel.DataAnnotations.Schema;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant within a client application that is managed by an owner.
/// </summary>
public class Tenant
{
    /// <summary>Primary identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner identifier.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Tenant name.</summary>
    public required string Name { get; set; }

    /// <summary>Tenant logo URL.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Tenant description.</summary>
    public string? Description { get; set; }

    /// <summary>Client application identifier.</summary>
    public required string ClientId { get; set; }

    /// <summary>Date when the tenant was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>User who created the tenant.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Date when the tenant was last modified.</summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>User who last modified the tenant.</summary>
    public string? ModifiedBy { get; set; }

    public Dictionary<string, object?> Metadata { get; set; } = [];

    /// <summary>Collection of tenant indicators (hostnames/subdomains).</summary>
    public ICollection<TenantIndicator> Indicators { get; set; } = [];
}
