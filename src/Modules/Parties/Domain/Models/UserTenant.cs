namespace Vitarish.Parties.Domain.Models;

/// <summary>
/// View model representing a user's tenant with its primary indicator.
/// Retrieved from a cross-schema view based on the current user's roles.
/// </summary>
public sealed class UserTenant
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
    public string IndicatorValue { get; init; }
}
