using Vitarish.Entities.Server.Core;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Entities;

/// <summary>
/// Represents a tenant identification indicator (hostname or subdomain).
/// </summary>
public class TenantIndicator : Entity
{
    private Guid _tenantId;
    private IndicatorMethod _method;
    private string _value = string.Empty;
    private IndicatorStatus _status;
    private bool _isPrimary;
    private VerificationInfo? _verification;
    private DateTimeOffset? _verifiedAt;
    private DateTimeOffset? _lastCheckedAt;

    /// <summary>Associated tenant identifier.</summary>
    public Guid TenantId
    {
        get => _tenantId;
        set => SetField(ref _tenantId, value, nameof(TenantId));
    }

    /// <summary>Associated tenant.</summary>
    public Tenant Tenant { get; set; } = default!;

    /// <summary>Indicator method (hostname or subdomain).</summary>
    public IndicatorMethod Method
    {
        get => _method;
        set => SetField(ref _method, value, nameof(Method));
    }

    /// <summary>Canonical FQDN value (normalized to lowercase punycode).</summary>
    public required string Value
    {
        get => _value;
        set => SetField(ref _value, value, nameof(Value));
    }

    /// <summary>Current lifecycle status.</summary>
    public IndicatorStatus Status
    {
        get => _status;
        set => SetField(ref _status, value, nameof(Status));
    }

    /// <summary>Whether this is the primary indicator for the tenant.</summary>
    public bool IsPrimary
    {
        get => _isPrimary;
        set => SetField(ref _isPrimary, value, nameof(IsPrimary));
    }

    /// <summary>Verification metadata.</summary>
    public VerificationInfo? Verification
    {
        get => _verification;
        set => SetField(ref _verification, value, nameof(Verification));
    }

    /// <summary>Date when the indicator was verified.</summary>
    public DateTimeOffset? VerifiedAt
    {
        get => _verifiedAt;
        set => SetField(ref _verifiedAt, value, nameof(VerifiedAt));
    }

    /// <summary>Date when the indicator was last checked.</summary>
    public DateTimeOffset? LastCheckedAt
    {
        get => _lastCheckedAt;
        set => SetField(ref _lastCheckedAt, value, nameof(LastCheckedAt));
    }
}
