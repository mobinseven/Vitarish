using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Application.Abstractions;

/// <summary>
/// Canonical adapter contract for insurance provider integration.
/// Each provider implements this interface; unsupported operations
/// return NotSupported result (not exceptions).
/// </summary>
public interface IInsuranceProviderAdapter
{
    /// <summary>Unique provider key for this adapter.</summary>
    string ProviderKey { get; }

    /// <summary>Get supported capabilities for this provider.</summary>
    IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities();

    /// <summary>Authenticate doctor credentials with the provider.</summary>
    Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Check patient entitlement/eligibility.</summary>
    Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Submit a prescription to the provider.</summary>
    Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fetch a previously submitted prescription's current state.</summary>
    Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Edit a previously accepted prescription.</summary>
    Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default);

    /// <summary>Cancel a previously accepted prescription.</summary>
    Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fetch reference data (drugs, services) for sync.</summary>
    Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result wrapper for adapter operations. Carries success/failure, error classification,
/// and optional payload.
/// </summary>
public record AdapterResult
{
    public bool IsSuccess { get; init; }
    public bool IsNotSupported { get; init; }
    public IntegrationError? Error { get; init; }

    public static AdapterResult Success() => new() { IsSuccess = true };
    public static AdapterResult NotSupported() => new() { IsNotSupported = true };
    public static AdapterResult Failure(IntegrationError error) => new() { Error = error };
}

/// <summary>
/// Typed result wrapper with payload.
/// </summary>
public record AdapterResult<T> : AdapterResult
{
    public T? Data { get; init; }

    public static AdapterResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static new AdapterResult<T> NotSupported() => new() { IsNotSupported = true };
    public static new AdapterResult<T> Failure(IntegrationError error) => new() { Error = error };
}

// Request/Response DTOs for the adapter contract

public sealed record AuthenticateRequest
{
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
}

public sealed record EntitlementRequest
{
    public required string PatientNationalId { get; init; }
    public required Guid DoctorId { get; init; }
    public required Guid ClinicId { get; init; }
}

public sealed record EntitlementResponse
{
    public required EligibilityStatus Status { get; init; }
    public string? TrackingCode { get; init; }
    public string? CoverageIdentifier { get; init; }
    public string? RawStatusCode { get; init; }
    public string? RawStatusMessage { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public sealed record SubmissionRequest
{
    public required Guid PrescriptionId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record SubmissionResponse
{
    public string? RemotePrescriptionId { get; init; }
    public string? RemoteTrackingCode { get; init; }
    public string? RemoteStatusCode { get; init; }
    public string? RemoteStatusText { get; init; }
    public PrescriptionStatus CanonicalStatus { get; init; }
}

public sealed record FetchRequest
{
    public string? RemotePrescriptionId { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed record FetchResponse
{
    public string? RemoteStatusCode { get; init; }
    public string? RemoteStatusText { get; init; }
    public PrescriptionStatus CanonicalStatus { get; init; }
    public string? RemoteTrackingCode { get; init; }
}

public sealed record EditRequest
{
    public required Guid PrescriptionId { get; init; }
    public required string IdempotencyKey { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record CancelRequest
{
    public required string RemotePrescriptionId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed record ReferenceDataRequest
{
    public required string CodeSystem { get; init; }
    public string? SinceVersion { get; init; }
}

public sealed record ReferenceDataResponse
{
    public required string CodeSystem { get; init; }
    public required string Version { get; init; }
    public required IReadOnlyList<ReferenceDataItem> Items { get; init; }
}

public sealed record ReferenceDataItem
{
    public required string Code { get; init; }
    public required string DisplayName { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; } = true;
}
