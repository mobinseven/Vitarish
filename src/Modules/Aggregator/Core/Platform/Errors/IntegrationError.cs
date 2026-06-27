namespace Vitarish.Modules.Aggregator.Core.Platform.Errors;

/// <summary>
/// Represents a classified error from an external integration.
/// Adapters map provider-specific errors into this canonical form.
/// </summary>
public sealed record IntegrationError
{
    /// <summary>Canonical error category.</summary>
    public required ErrorCategory Category { get; init; }

    /// <summary>Clinician-safe summary message (no technical details or sensitive data).</summary>
    public required string UserMessage { get; init; }

    /// <summary>Technical detail for logs/support (may contain provider-specific codes).</summary>
    public string? TechnicalDetail { get; init; }

    /// <summary>Provider-native error code preserved for audit/support.</summary>
    public string? ProviderErrorCode { get; init; }

    /// <summary>Provider-native error message preserved for audit/support.</summary>
    public string? ProviderErrorMessage { get; init; }

    /// <summary>Whether the operation can be safely retried.</summary>
    public bool IsRetryable { get; init; }
}
