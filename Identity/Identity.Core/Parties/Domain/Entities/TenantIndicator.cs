using Vitarish.Base.Domain.Primitives;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Core.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant identification indicator (hostname or subdomain).
/// </summary>
public sealed class TenantIndicator : Entity<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIndicator"/> class.
    /// Required for EF Core.
    /// </summary>
    private TenantIndicator() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantIndicator"/> class.
    /// </summary>
    public TenantIndicator(
        Guid id,
        Guid tenantId,
        IndicatorMethod method,
        string value,
        IndicatorStatus status,
        bool isPrimary,
        VerificationInfo? verification)
    {
        Id = id;
        TenantId = tenantId;
        Method = method;
        Value = value;
        Status = status;
        IsPrimary = isPrimary;
        Verification = verification;
    }

    /// <summary>Associated tenant identifier.</summary>
    public Guid TenantId { get; private set; }

    /// <summary>Associated tenant.</summary>
    public Tenant? Tenant { get; private set; }

    /// <summary>Indicator method (hostname or subdomain).</summary>
    public IndicatorMethod Method { get; private set; }

    /// <summary>Canonical FQDN value (normalized to lowercase punycode).</summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public IndicatorStatus Status { get; private set; }

    /// <summary>Whether this is the primary indicator for the tenant.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>Verification metadata.</summary>
    public VerificationInfo? Verification { get; private set; }

    /// <summary>Date when the indicator was verified.</summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <summary>Date when the indicator was last checked.</summary>
    public DateTimeOffset? LastCheckedAt { get; private set; }

    /// <summary>
    /// Sets verification metadata and marks the indicator as verified.
    /// </summary>
    public void MarkVerified(VerificationInfo verification, DateTimeOffset? verifiedAt = null)
    {
        Verification = verification;
        Status = IndicatorStatus.Verified;
        VerifiedAt = verifiedAt ?? DateTimeOffset.UtcNow;
        MarkAsUpdated();
    }

    /// <summary>
    /// Activates the indicator for routing traffic.
    /// </summary>
    public void Activate()
    {
        Status = IndicatorStatus.Active;
        MarkAsUpdated();
    }

    /// <summary>
    /// Disables the indicator and stops routing traffic.
    /// </summary>
    public void Disable()
    {
        Status = IndicatorStatus.Disabled;
        MarkAsUpdated();
    }

    /// <summary>
    /// Records the last time the indicator was checked.
    /// </summary>
    public void UpdateLastChecked(DateTimeOffset? checkedAt = null)
    {
        LastCheckedAt = checkedAt ?? DateTimeOffset.UtcNow;
        MarkAsUpdated();
    }
}
