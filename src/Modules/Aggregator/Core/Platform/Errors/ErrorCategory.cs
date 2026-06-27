namespace Vitarish.Modules.Aggregator.Core.Platform.Errors;

/// <summary>
/// Platform-level canonical error taxonomy. Provider adapters and external integrations
/// map their errors into this taxonomy for consistent UI rendering and operational handling.
/// </summary>
public enum ErrorCategory
{
    /// <summary>Credential or token failure when calling external service.</summary>
    Authentication,

    /// <summary>Caller lacks permission for the requested operation.</summary>
    Authorization,

    /// <summary>Input data fails structural or format validation.</summary>
    Validation,

    /// <summary>External service rejected request based on business rules.</summary>
    BusinessRule,

    /// <summary>Request timed out before receiving a response.</summary>
    Timeout,

    /// <summary>External service is unavailable or experiencing an outage.</summary>
    Outage,

    /// <summary>Operation may have created a duplicate; requires investigation.</summary>
    DuplicateRisk,

    /// <summary>Request was rejected due to rate limiting.</summary>
    RateLimit,

    /// <summary>Error cannot be classified into a known category.</summary>
    Unknown
}
