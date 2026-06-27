using Vitarish.Modules.Aggregator.Core.Platform.Errors;

namespace Vitarish.Medical.Application.Abstractions.Prescriptions;

/// <summary>
/// Maps provider-specific error codes and responses to the canonical error taxonomy.
/// Each provider adapter implements this to normalize its native errors.
/// </summary>
public interface IProviderErrorMapper
{
    /// <summary>The provider key this mapper handles.</summary>
    string ProviderKey { get; }

    /// <summary>
    /// Maps a provider-native error response to a canonical IntegrationError.
    /// </summary>
    IntegrationError MapError(ProviderErrorContext context);
}

/// <summary>
/// Context containing provider-native error information for mapping.
/// </summary>
public sealed record ProviderErrorContext
{
    /// <summary>HTTP status code from the provider response.</summary>
    public int? HttpStatusCode { get; init; }

    /// <summary>Provider-native error code.</summary>
    public string? ProviderErrorCode { get; init; }

    /// <summary>Provider-native error message.</summary>
    public string? ProviderErrorMessage { get; init; }

    /// <summary>The operation that failed (e.g., "CheckEntitlement", "SubmitPrescription").</summary>
    public string? OperationName { get; init; }

    /// <summary>Exception if the error was caused by a transport failure.</summary>
    public Exception? Exception { get; init; }
}
