namespace Vitarish.Identity.Core.Domain.Enums;

/// <summary>
/// Lifecycle status of a tenant indicator.
/// </summary>
public enum IndicatorStatus
{
    /// <summary>Awaiting verification.</summary>
    PendingVerification = 1,

    /// <summary>Verified but not yet active.</summary>
    Verified = 2,

    /// <summary>Active and routing traffic.</summary>
    Active = 3,

    /// <summary>Disabled, no longer in use.</summary>
    Disabled = 4
}
