using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant identification indicator (hostname or subdomain).
/// </summary>
public sealed class TenantIndicator
{
    /// <summary>Primary identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Associated tenant identifier.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Associated tenant.</summary>
    public Tenant Tenant { get; set; } = default!;

    /// <summary>Indicator method (hostname or subdomain).</summary>
    public IndicatorMethod Method { get; set; }

    /// <summary>Canonical FQDN value (normalized to lowercase punycode).</summary>
    public required string Value { get; set; }

    /// <summary>Current lifecycle status.</summary>
    public IndicatorStatus Status { get; set; }

    /// <summary>Whether this is the primary indicator for the tenant.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Verification metadata.</summary>
    public VerificationInfo? Verification { get; set; }

    /// <summary>Date when the indicator was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Date when the indicator was verified.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }

    /// <summary>Date when the indicator was last checked.</summary>
    public DateTimeOffset? LastCheckedAt { get; set; }
}
