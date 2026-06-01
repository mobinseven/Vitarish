using System.Text.Json;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Entities;
using DomainPrescriptionStatus = Vitarish.Prescription.Domain.Enums.PrescriptionStatus;
using EligibilityStatus = Vitarish.Prescription.Domain.Enums.EligibilityStatus;
using PrescriptionItemCategory = Vitarish.Prescription.Domain.Enums.PrescriptionItemCategory;
using Vorn.Tamin;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

internal static class TaminPrescriptionMapper
{
    private static readonly JsonElement EmptyElement = JsonDocument.Parse("{}").RootElement;

    public static Task<JsonElement> RegisterPrescriptionAsync(
        TaminSession session,
        PrescriptionRecord prescription,
        string doctorTaminId,
        CancellationToken cancellationToken)
    {
        var patientNationalId = prescription.PatientNationalId ?? prescription.PatientId.ToString();
        var visitDate = (prescription.SubmittedAt ?? prescription.CreatedAt).ToString("yyyy-MM-dd");

        var dominantCategory = prescription.Items
            .GroupBy(i => i.Category)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        return dominantCategory switch
        {
            PrescriptionItemCategory.Drug => session.Prescription.RegisterDrugPrescriptionAsync(
                new RegisterDrugPrescriptionRequest
                {
                    DoctorId = doctorTaminId,
                    PatientNationalId = patientNationalId,
                    VisitDate = visitDate,
                    DrugItems = prescription.Items
                        .Where(i => i.Category == PrescriptionItemCategory.Drug)
                        .Select(i => new DrugItem
                        {
                            DrugCode = i.ReferenceCode ?? string.Empty,
                            Quantity = i.Quantity,
                            DosageInstruction = i.DosageJson,
                            Duration = i.DurationDays?.ToString()
                        })
                        .ToList()
                }, cancellationToken),

            PrescriptionItemCategory.Physiotherapy => session.Prescription.RegisterPhysiotherapyPrescriptionAsync(
                new RegisterPhysiotherapyPrescriptionRequest
                {
                    DoctorId = doctorTaminId,
                    PatientNationalId = patientNationalId,
                    PhysiotherapyItems = prescription.Items
                        .Where(i => i.Category == PrescriptionItemCategory.Physiotherapy)
                        .Select(i => new PhysiotherapyItem
                        {
                            ServiceCode = i.ReferenceCode ?? string.Empty,
                            Description = i.DisplayName
                        })
                        .ToList()
                }, cancellationToken),

            PrescriptionItemCategory.Visit => session.Prescription.RegisterVisitPrescriptionAsync(
                new RegisterVisitPrescriptionRequest
                {
                    DoctorId = doctorTaminId,
                    PatientNationalId = patientNationalId,
                    VisitDate = visitDate,
                    ClinicId = prescription.ClinicId.ToString()
                }, cancellationToken),

            PrescriptionItemCategory.MedicalService => session.Prescription.RegisterMedicalServicePrescriptionAsync(
                new RegisterMedicalServicePrescriptionRequest
                {
                    DoctorId = doctorTaminId,
                    PatientNationalId = patientNationalId,
                    VisitDate = visitDate,
                    ServiceItems = prescription.Items
                        .Where(i => i.Category == PrescriptionItemCategory.MedicalService)
                        .Select(i => new ServiceItem
                        {
                            ServiceCode = i.ReferenceCode ?? string.Empty,
                            Quantity = i.Quantity
                        })
                        .ToList()
                }, cancellationToken),

            // Lab, Imaging, Paraclinic, Dental, MedicalEquipment -> paraclinic
            _ => session.Prescription.RegisterParaclinicPrescriptionAsync(
                new RegisterParaclinicPrescriptionRequest
                {
                    DoctorId = doctorTaminId,
                    PatientNationalId = patientNationalId,
                    VisitDate = visitDate,
                    ServiceItems = prescription.Items
                        .Select(i => new ServiceItem
                        {
                            ServiceCode = i.ReferenceCode ?? string.Empty,
                            Quantity = i.Quantity
                        })
                        .ToList()
                }, cancellationToken)
        };
    }

    public static EntitlementResponse ToEntitlementResponse(JsonElement result) => new()
    {
        Status = MapTaminEligibility(GetString(result, "status_code")),
        TrackingCode = GetString(result, "tracking_code"),
        CoverageIdentifier = GetString(result, "insurance_number"),
        RawStatusCode = GetString(result, "status_code"),
        RawStatusMessage = GetString(result, "status_message"),
        ExpiresAt = GetDateTimeOffset(result, "expires_at")
    };

    public static SubmissionResponse ToSubmissionResponse(JsonElement result, string? fallbackRemotePrescriptionId = null) => new()
    {
        RemotePrescriptionId = GetString(result, "prescription_id") ?? fallbackRemotePrescriptionId,
        RemoteTrackingCode = GetString(result, "tracking_code"),
        RemoteStatusCode = GetString(result, "status_code"),
        RemoteStatusText = GetString(result, "status_message"),
        CanonicalStatus = MapTaminSubmissionStatus(GetString(result, "status_code"))
    };

    public static FetchResponse ToFetchResponse(JsonElement result) => new()
    {
        RemoteStatusCode = GetString(result, "status_code"),
        RemoteStatusText = GetString(result, "status_message"),
        CanonicalStatus = MapTaminSubmissionStatus(GetString(result, "status_code")),
        RemoteTrackingCode = GetString(result, "tracking_code")
    };

    public static ReferenceDataResponse ToReferenceDataResponse(ReferenceDataRequest request, JsonElement result) => new()
    {
        CodeSystem = request.CodeSystem,
        Version = GetString(result, "version") ?? "unknown",
        Items = EnumerateArray(result)
            .Select(item => new ReferenceDataItem
            {
                Code = GetString(item, "drug_code") ?? GetString(item, "service_code") ?? string.Empty,
                DisplayName = GetString(item, "drug_name") ?? GetString(item, "service_name") ?? string.Empty,
                Category = GetString(item, "category"),
                IsActive = GetBool(item, "is_active") ?? true
            })
            .ToList()
    };

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static bool? GetBool(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
            return prop.GetBoolean();
        return null;
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement element, string propertyName)
    {
        var value = GetString(element, propertyName);
        return DateTimeOffset.TryParse(value, out var dt) ? dt : null;
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
            return element.EnumerateArray();
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("items", out var items) &&
            items.ValueKind == JsonValueKind.Array)
            return items.EnumerateArray();
        return [];
    }

    private static EligibilityStatus MapTaminEligibility(string? statusCode) => statusCode switch
    {
        "ELIGIBLE" or "1" => EligibilityStatus.Eligible,
        "NOT_ELIGIBLE" or "0" => EligibilityStatus.NotEligible,
        "REQUIRES_CONFIRMATION" => EligibilityStatus.RequiresPatientConfirmation,
        _ => EligibilityStatus.Unknown
    };

    private static DomainPrescriptionStatus MapTaminSubmissionStatus(string? statusCode) => statusCode switch
    {
        "ACCEPTED" or "1" => DomainPrescriptionStatus.AcceptedByProvider,
        "REJECTED" or "0" => DomainPrescriptionStatus.RejectedByProvider,
        "PENDING" or "2" => DomainPrescriptionStatus.Submitted,
        _ => DomainPrescriptionStatus.SyncPending
    };
}
