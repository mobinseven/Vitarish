using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.TaminInsurance.Client;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

internal static class TaminPrescriptionMapper
{
    public static TaminPrescriptionSubmitRequest ToTaminSubmission(PrescriptionRecord prescription)
    {
        return new TaminPrescriptionSubmitRequest
        {
            PatientId = prescription.PatientId.ToString(),
            Items = prescription.Items.Select(i => new TaminPrescriptionItem
            {
                Code = i.ReferenceCode ?? string.Empty,
                Name = i.DisplayName ?? string.Empty,
                Category = MapItemCategory(i.Category),
                Quantity = i.Quantity,
                DurationDays = i.DurationDays,
                DosageInfo = i.DosageJson
            }).ToList()
        };
    }

    public static EntitlementResponse ToEntitlementResponse(TaminEntitlementResponse? result) => new()
    {
        Status = MapTaminEligibility(result?.StatusCode),
        TrackingCode = result?.TrackingCode,
        CoverageIdentifier = result?.InsuranceNumber,
        RawStatusCode = result?.StatusCode,
        RawStatusMessage = result?.StatusMessage,
        ExpiresAt = result?.ExpiresAt
    };

    public static SubmissionResponse ToSubmissionResponse(TaminSubmissionResponse? result, string? fallbackRemotePrescriptionId = null) => new()
    {
        RemotePrescriptionId = result?.PrescriptionId ?? fallbackRemotePrescriptionId,
        RemoteTrackingCode = result?.TrackingCode,
        RemoteStatusCode = result?.StatusCode,
        RemoteStatusText = result?.StatusMessage,
        CanonicalStatus = MapTaminSubmissionStatus(result?.StatusCode)
    };

    public static FetchResponse ToFetchResponse(TaminFetchResponse? result) => new()
    {
        RemoteStatusCode = result?.StatusCode,
        RemoteStatusText = result?.StatusMessage,
        CanonicalStatus = MapTaminSubmissionStatus(result?.StatusCode),
        RemoteTrackingCode = result?.TrackingCode
    };

    public static ReferenceDataResponse ToReferenceDataResponse(ReferenceDataRequest request, TaminReferenceDataResponse? result) => new()
    {
        CodeSystem = request.CodeSystem,
        Version = result?.Version ?? "unknown",
        Items = result?.Items?.Select(i => new ReferenceDataItem
        {
            Code = i.Code,
            DisplayName = i.Name,
            Category = i.Category,
            IsActive = i.IsActive
        }).ToList() ?? []
    };

    private static string MapItemCategory(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Visit => "visit",
        PrescriptionItemCategory.Drug => "drug",
        PrescriptionItemCategory.Lab => "lab",
        PrescriptionItemCategory.Imaging => "imaging",
        PrescriptionItemCategory.Paraclinic => "paraclinic",
        PrescriptionItemCategory.MedicalService => "service",
        PrescriptionItemCategory.Physiotherapy => "physio",
        PrescriptionItemCategory.Dental => "dental",
        PrescriptionItemCategory.Referral => "referral",
        PrescriptionItemCategory.MedicalEquipment => "equipment",
        _ => "other"
    };

    private static EligibilityStatus MapTaminEligibility(string? statusCode) => statusCode switch
    {
        "ELIGIBLE" or "1" => EligibilityStatus.Eligible,
        "NOT_ELIGIBLE" or "0" => EligibilityStatus.NotEligible,
        "REQUIRES_CONFIRMATION" => EligibilityStatus.RequiresPatientConfirmation,
        _ => EligibilityStatus.Unknown
    };

    private static PrescriptionStatus MapTaminSubmissionStatus(string? statusCode) => statusCode switch
    {
        "ACCEPTED" or "1" => PrescriptionStatus.AcceptedByProvider,
        "REJECTED" or "0" => PrescriptionStatus.RejectedByProvider,
        "PENDING" or "2" => PrescriptionStatus.Submitted,
        _ => PrescriptionStatus.SyncPending
    };
}
