using Vitarish.DitasInsurance.Client;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Providers.Ditas;

internal static class DitasPrescriptionMapper
{
    public static DitasPrescriptionSubmitRequest ToDitasSubmission(SubmissionRequest request) => new()
    {
        PrescriptionId = request.PrescriptionId.ToString("N"),
        IdempotencyKey = request.IdempotencyKey,
        CorrelationId = request.CorrelationId
    };

    public static EntitlementResponse ToEntitlementResponse(DitasEntitlementResponse? result) => new()
    {
        Status = MapDitasEligibility(result?.Status),
        TrackingCode = result?.TrackingCode,
        CoverageIdentifier = result?.MembershipNumber,
        RawStatusCode = result?.Status,
        RawStatusMessage = result?.StatusDescription,
        ExpiresAt = result?.ValidUntil?.UtcDateTime
    };

    public static SubmissionResponse ToSubmissionResponse(DitasSubmissionResponse? result) => new()
    {
        RemotePrescriptionId = result?.PrescriptionNumber,
        RemoteTrackingCode = result?.TrackingCode,
        RemoteStatusCode = result?.Status,
        RemoteStatusText = result?.StatusDescription,
        CanonicalStatus = PrescriptionStatus.AcceptedByProvider
    };

    public static FetchResponse ToFetchResponse(DitasFetchResponse? result) => new()
    {
        RemoteStatusCode = result?.Status,
        RemoteStatusText = result?.StatusDescription,
        CanonicalStatus = MapDitasPrescriptionStatus(result?.Status),
        RemoteTrackingCode = result?.TrackingCode
    };

    private static EligibilityStatus MapDitasEligibility(string? status) => status switch
    {
        "ELIGIBLE" or "COVERED" => EligibilityStatus.Eligible,
        "NOT_ELIGIBLE" or "NOT_COVERED" => EligibilityStatus.NotEligible,
        "PENDING" => EligibilityStatus.RequiresManualVerification,
        _ => EligibilityStatus.Unknown
    };

    private static PrescriptionStatus MapDitasPrescriptionStatus(string? status) => status switch
    {
        "ACCEPTED" or "REGISTERED" => PrescriptionStatus.AcceptedByProvider,
        "CANCELLED" => PrescriptionStatus.Cancelled,
        "SUBMITTED" or "PENDING" => PrescriptionStatus.Submitted,
        _ => PrescriptionStatus.SyncPending
    };
}
