using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant identification indicator (hostname or subdomain).
/// </summary>
public class TenantIndicator : Entity
{
    /// <summary>Associated tenant identifier.</summary>
    public Guid TenantId
    {
        get;
        set => SetField(ref field, value, nameof(TenantId));
    }

    /// <summary>Associated tenant.</summary>
    public Tenant Tenant { get; set; } = default!;

    /// <summary>Indicator method (hostname or subdomain).</summary>
    public IndicatorMethod Method
    {
        get;
        set => SetField(ref field, value, nameof(Method));
    }

    /// <summary>Canonical FQDN value (normalized to lowercase punycode).</summary>
    public required string Value
    {
        get;
        set => SetField(ref field, value, nameof(Value));
    } = string.Empty;

    /// <summary>Current lifecycle status.</summary>
    public IndicatorStatus Status
    {
        get;
        set => SetField(ref field, value, nameof(Status));
    }

    /// <summary>Whether this is the primary indicator for the tenant.</summary>
    public bool IsPrimary
    {
        get;
        set => SetField(ref field, value, nameof(IsPrimary));
    }

    /// <summary>Verification metadata.</summary>
    public VerificationInfo? Verification
    {
        get;
        set => SetField(ref field, value, nameof(Verification));
    }

    /// <summary>Date when the indicator was verified.</summary>
    public DateTimeOffset? VerifiedAt
    {
        get;
        set => SetField(ref field, value, nameof(VerifiedAt));
    }

    /// <summary>Date when the indicator was last checked.</summary>
    public DateTimeOffset? LastCheckedAt
    {
        get;
        set => SetField(ref field, value, nameof(LastCheckedAt));
    }
}
