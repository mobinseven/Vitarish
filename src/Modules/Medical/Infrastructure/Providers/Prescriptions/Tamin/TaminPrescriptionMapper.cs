using System.Text.Json;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vorn.Tamin;
using DomainPrescriptionStatus = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionStatus;
using EligibilityStatus = Vitarish.Medical.Domain.Enums.Prescriptions.EligibilityStatus;
using PrescriptionItemCategory = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionItemCategory;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal static class TaminPrescriptionMapper
{
    public static Task<JsonElement> RegisterPrescriptionAsync(
        TaminSession session,
        PrescriptionRecord prescription,
        string doctorTaminId,
        ITaminDateFormatter dateFormatter,
        CancellationToken cancellationToken)
    {
        string patientNationalId = prescription.PatientNationalId ?? prescription.PatientId.ToString();
        string visitDate = dateFormatter.FormatDate(prescription.SubmittedAt ?? prescription.CreatedAt);

        PrescriptionItemCategory dominantCategory = prescription.Items
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

            PrescriptionItemCategory.Physiotherapy => RegisterPhysiotherapyAsync(
                session,
                prescription,
                doctorTaminId,
                patientNationalId,
                visitDate,
                cancellationToken),

            PrescriptionItemCategory.Referral => RegisterReferralAsync(
                session,
                prescription,
                doctorTaminId,
                patientNationalId,
                visitDate,
                cancellationToken),

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

    private static Task<JsonElement> RegisterPhysiotherapyAsync(
        TaminSession session,
        PrescriptionRecord prescription,
        string doctorTaminId,
        string patientNationalId,
        string visitDate,
        CancellationToken cancellationToken)
    {
        List<PrescriptionItem> items = prescription.Items
            .Where(i => i.Category == PrescriptionItemCategory.Physiotherapy)
            .OrderBy(i => i.SortOrder)
            .ToList();
        PrescriptionItem? first = items.FirstOrDefault();

        return session.Prescription.RegisterPhysiotherapyPrescriptionAsync(
            new RegisterPhysiotherapyPrescriptionRequest
            {
                DoctorId = doctorTaminId,
                PatientNationalId = patientNationalId,
                EffectiveDate = TaminPrescriptionItemMetadataReader.ReadString(first, "effectiveDate", "effective_date") ?? visitDate,
                SessionCount = first is null ? items.Sum(i => Math.Max(1, i.Quantity)) : TaminPrescriptionItemMetadataReader.ReadPositiveInt(first, "sessionCount", "session_count") ?? items.Sum(i => Math.Max(1, i.Quantity)),
                Description = first?.DisplayName,
                PhysiotherapyItems = items
                    .Select(i => new PhysiotherapyItem
                    {
                        ServiceCode = i.ReferenceCode ?? string.Empty,
                        Description = i.DisplayName
                    })
                    .ToList()
            }, cancellationToken);
    }

    private static Task<JsonElement> RegisterReferralAsync(
        TaminSession session,
        PrescriptionRecord prescription,
        string doctorTaminId,
        string patientNationalId,
        string visitDate,
        CancellationToken cancellationToken)
    {
        PrescriptionItem? item = prescription.Items
            .Where(i => i.Category == PrescriptionItemCategory.Referral)
            .OrderBy(i => i.SortOrder)
            .FirstOrDefault();

        return session.Prescription.RegisterReferralPrescriptionAsync(
            new RegisterReferralPrescriptionRequest
            {
                DoctorId = doctorTaminId,
                PatientNationalId = patientNationalId,
                VisitDate = visitDate,
                TargetSpecialty = item?.ReferenceCode ?? TaminPrescriptionItemMetadataReader.ReadString(item, "targetSpecialty", "target_specialty") ?? string.Empty,
                TargetProviderType = TaminPrescriptionItemMetadataReader.ReadString(item, "targetProviderType", "target_provider_type") ?? string.Empty,
                Reason = TaminPrescriptionItemMetadataReader.ReadString(item, "reason", "referralReason", "referral_reason") ?? item?.DisplayName ?? string.Empty,
                Description = TaminPrescriptionItemMetadataReader.ReadString(item, "description") ?? item?.DisplayName ?? string.Empty
            }, cancellationToken);
    }

    public static EntitlementResponse ToEntitlementResponse(JsonElement result) => new()
    {
        Status = MapTaminEligibility(GetString(result, "status_code")),
        TrackingCode = GetString(result, "tracking_code"),
        CoverageIdentifier = GetString(result, "insurance_number"),
        RawStatusCode = GetString(result, "status_code"),
        RawStatusMessage = GetString(result, "status_message"),
        ExpiresAt = GetDateTime(result, "expires_at")
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

    private static string? GetString(JsonElement element, string propertyName) => element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out JsonElement prop) &&
            prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static bool? GetBool(JsonElement element, string propertyName) => element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out JsonElement prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
            ? prop.GetBoolean()
            : null;

    private static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        string? value = GetString(element, propertyName);
        return DateTimeOffset.TryParse(value, out DateTimeOffset parsedOffset) ? parsedOffset.UtcDateTime : null;
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element) => element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray()
            : element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("items", out JsonElement items) &&
            items.ValueKind == JsonValueKind.Array
            ? items.EnumerateArray()
            : (IEnumerable<JsonElement>)[];

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
