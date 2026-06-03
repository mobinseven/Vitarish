namespace Vitarish.Parties.Infrastructure.ViewModels;

/// <summary>
/// View model representing a user's tenant with its primary indicator.
/// Optimized for retrieving user tenants in a single database query.
/// </summary>
public sealed class UserTenantView
{
    /// <summary>Tenant identifier.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Tenant name.</summary>
    public required string TenantName { get; init; }

    /// <summary>Tenant logo URL.</summary>
    public string? TenantLogoUrl { get; init; }

    /// <summary>Tenant description.</summary>
    public string? TenantDescription { get; init; }

    /// <summary>Tenant application identifier.</summary>
    public required string TenantAppId { get; init; }

    /// <summary>Primary indicator identifier.</summary>
    public Guid? IndicatorId { get; init; }

    /// <summary>Primary indicator value (hostname/subdomain).</summary>
    public string? IndicatorValue { get; init; }

    /// <summary>Tenant metadata.</summary>
    public Dictionary<string, object?> Metadata { get; init; } = [];
}
