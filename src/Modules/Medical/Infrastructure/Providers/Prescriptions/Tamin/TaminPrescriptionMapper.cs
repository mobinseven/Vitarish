using System.Text.Json;
using Tamin.Integration;
using Tamin.Integration.Mapping;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using DomainPrescriptionStatus = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionStatus;
using EligibilityStatus = Vitarish.Medical.Domain.Enums.Prescriptions.EligibilityStatus;
using PrescriptionItemCategory = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionItemCategory;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminPrescriptionMapper
{
    /// <summary>Coding system the guide asks for on drug prescriptions that carry SNOMED consumption values.</summary>
    private const string SnomedTerminology = "SNOMED";

    private static readonly string[] DrugAmountMetadataKeys = ["drugAmntId", "drug_amnt_id"];
    private static readonly string[] DrugInstructionMetadataKeys = ["drugInstId", "drug_inst_id"];

    public async Task<SubmissionResponse> RegisterPrescriptionAsync(
        ITaminClient client,
        PrescriptionRecord prescription,
        string doctorTaminId,
        string doctorNationalCode,
        string? clinicSiamId,
        string? mobile,
        ITaminDateFormatter dateFormatter,
        CancellationToken cancellationToken)
    {
        PrescriptionCreateInput input = CreateInput(
            prescription,
            doctorTaminId,
            doctorNationalCode,
            clinicSiamId,
            mobile,
            dateFormatter);

        PrescriptionMutationResult result = await client.CreatePrescriptionAsync(input, cancellationToken);

        string? statusCode = result.ErrorCode;
        return new SubmissionResponse
        {
            RemotePrescriptionId = result.HeaderId?.ToString(),
            RemoteTrackingCode = result.TrackingCode?.ToString(),
            RemoteStatusCode = statusCode,
            RemoteStatusText = result.ErrorMessage ?? result.ComplementaryMessage,
            CanonicalStatus = string.IsNullOrWhiteSpace(statusCode)
                ? DomainPrescriptionStatus.AcceptedByProvider
                : DomainPrescriptionStatus.RejectedByProvider
        };
    }

    /// <summary>
    /// The guide makes the consumption amount and time mandatory on every drug item. Returns the display name of
    /// the first drug item without them, so the doctor sees which drug to complete instead of a provider rejection.
    /// </summary>
    public static string? FindDrugMissingConsumption(IEnumerable<PrescriptionItem> items) =>
        items
            .Where(item => item.Category == PrescriptionItemCategory.Drug &&
                (TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, DrugAmountMetadataKeys) is null ||
                 TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, DrugInstructionMetadataKeys) is null))
            .Select(item => item.DisplayName ?? item.ReferenceCode ?? string.Empty)
            .FirstOrDefault();

    public async Task<DrugConsumptionOptions> GetDrugConsumptionOptionsAsync(
        ITaminClient client,
        string formCode,
        CancellationToken cancellationToken)
    {
        TaminEnvelope<MedicationInstructionsDto> response = await client.GetMedicationInstructionsByDosageFormAsync(formCode, cancellationToken);
        MedicationInstructionsDto data = response.Data;
        return new DrugConsumptionOptions
        {
            Units = (data.FormAmounts ?? [])
                .Where(unit => !string.IsNullOrWhiteSpace(unit.Id))
                .Select(unit => new DrugConsumptionUnit(unit.Id!, unit.Name ?? unit.Id!, unit.NameLatin, ToOptions(unit.DrugAmounts)))
                .ToList(),
            Instructions = ToOptions(data.DrugInstructions),
            Usages = ToOptions(data.DrugUsages),
            Methods = ToOptions(data.MethodUsages)
        };
    }

    private static IReadOnlyList<DrugConsumptionOption> ToOptions(IReadOnlyList<MedicationInstructionOptionDto>? options) =>
        (options ?? [])
            .Where(option => !string.IsNullOrWhiteSpace(option.Id))
            .Select(option => new DrugConsumptionOption(option.Id!, option.Name ?? option.Id!, option.NameLatin))
            .ToList();

    public async Task<EntitlementResponse> CheckEntitlementAsync(
        ITaminClient client,
        string siamId,
        string doctorId,
        string doctorNationalCode,
        string patientNationalId,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await client.GetPatientEntitlementAsync(
            siamId,
            doctorId,
            patientNationalId,
            doctorNationalCode,
            cancellationToken);
        return ToEntitlementResponse(response.Payload);
    }

    public async Task<FetchResponse> FetchPrescriptionAsync(
        ITaminClient client,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await client.GetPrescriptionDetailsAsync(
            remotePrescriptionId,
            doctorNationalCode,
            doctorId,
            cancellationToken);
        return ToFetchResponse(response.Payload);
    }

    public async Task<SubmissionResponse> EditPrescriptionAsync(
        ITaminClient client,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        IEnumerable<PrescriptionItem> items,
        CancellationToken cancellationToken)
    {
        UnconstrainedResponse response = await client.UpdatePrescriptionAsync(
            remotePrescriptionId,
            doctorNationalCode,
            doctorId,
            items.OrderBy(item => item.SortOrder).Select(ToDetailInput).ToList(),
            cancellationToken);
        return ToSubmissionResponse(response.Payload, remotePrescriptionId.ToString());
    }

    public async Task CancelPrescriptionAsync(
        ITaminClient client,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        CancellationToken cancellationToken) =>
        _ = await client.DeletePrescriptionAsync(
            remotePrescriptionId,
            doctorNationalCode,
            doctorId,
            cancellationToken);

    private static PrescriptionCreateInput CreateInput(
        PrescriptionRecord prescription,
        string doctorTaminId,
        string doctorNationalCode,
        string? clinicSiamId,
        string? mobile,
        ITaminDateFormatter dateFormatter)
    {
        string patientNationalId = prescription.PatientNationalId ?? prescription.PatientId.ToString();
        string prescriptionDate = dateFormatter.FormatDate(prescription.SubmittedAt ?? prescription.CreatedAt);
        PrescriptionItemCategory category = prescription.Items
            .Select(item => item.Category)
            .FirstOrDefault();

        IReadOnlyList<PrescriptionReferralInput>? referrals = category == PrescriptionItemCategory.Referral
            ? prescription.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => new PrescriptionReferralInput(
                    Complaints: null,
                    DocSpecReferred: new SpecialInput(
                        item.ReferenceCode
                        ?? TaminPrescriptionItemMetadataReader.ReadString(item, "targetSpecialty", "target_specialty")),
                    Icd10s: null,
                    Id: null,
                    Message: TaminPrescriptionItemMetadataReader.ReadString(item, "reason", "referralReason", "referral_reason")
                        ?? item.DisplayName,
                    NoteDetailsEprscId: null,
                    PatientNationalCode: patientNationalId,
                    Quantity: item.Quantity,
                    ReferenceStatus: null,
                    ReferralFeedbackList: null,
                    ReferralHijriDate: prescriptionDate,
                    SiamId: clinicSiamId))
                .ToList()
            : null;

        IReadOnlyList<PrescriptionDetailInput> details = category is PrescriptionItemCategory.Visit or PrescriptionItemCategory.Referral
            ? []
            : prescription.Items.OrderBy(item => item.SortOrder).Select(ToDetailInput).ToList();

        return new PrescriptionCreateInput(
            Comments: null,
            CreatorType: null,
            DocId: doctorTaminId,
            DocMobileNo: null,
            DocNationalCode: doctorNationalCode,
            ExpireDate: null,
            Mobile: mobile,
            NoteDetailEprscs: details,
            NoteDetailsReferralList: referrals,
            NoteHeadEprscSupplementary: null,
            Patient: patientNationalId,
            PrescDate: prescriptionDate,
            PrescType: new PrescriptionTypeInput(ToPrescriptionType(category)),
            ReferralFeedbackId: null,
            SiamId: null,
            Terminology: prescription.Items.Any(IsSnomedDrug) ? SnomedTerminology : null);
    }

    /// <summary>
    /// Maps one stored item. Drug consumption values (amount, time, route, method) are optional item metadata,
    /// like the illness, plan and tooth ids; the provider rejects a drug whose values do not fit its dosage form.
    /// </summary>
    private static PrescriptionDetailInput ToDetailInput(PrescriptionItem item)
    {
        string? doseCode = ReadDoseCode(item);
        int? drugAmountId = TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, DrugAmountMetadataKeys);
        int? drugInstructionId = TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, DrugInstructionMetadataKeys);
        int? methodUsageId = TaminPrescriptionItemMetadataReader.ReadPositiveInt(item, "methodUsageId", "method_usage_id");

        return new PrescriptionDetailInput(
            DateDo: null,
            // For SNOMED drugs the route travels as doseCode instead of free-text dose.
            Dose: doseCode is null ? item.DosageJson : null,
            DoseCode: doseCode,
            DrugInstruction: drugInstructionId is null ? null : new DrugInstructionInput(drugInstructionId),
            IllnessId: TaminPrescriptionItemMetadataReader.ReadString(item, "illnessId", "illness_id"),
            IsDentalService: item.Category == PrescriptionItemCategory.Dental
                ? DentalServiceFlag.One
                : DentalServiceFlag.Zero,
            MethodUsage: methodUsageId is null ? null : new MethodUsageInput(methodUsageId),
            NoteDetailDrug: null,
            NoteDetailsEprscId: ParsePositiveLong(item.ProviderItemIdentifier),
            PlanId: TaminPrescriptionItemMetadataReader.ReadString(item, "planId", "plan_id"),
            ReferenceStatus: null,
            Repeat: item.DurationDays?.ToString(),
            SrvId: new ServiceInput(
                item.ReferenceCode,
                Terminology: null,
                SrvType: TaminPrescriptionItemMetadataReader.ReadString(item, "serviceType", "service_type")
                    ?? ToServiceType(item.Category)),
            SrvQty: item.Quantity,
            TimesAday: drugAmountId is null ? null : new DrugAmountInput(drugAmountId),
            ToothId: TaminPrescriptionItemMetadataReader.ReadString(item, "toothId", "tooth_id"));
    }

    private static string? ReadDoseCode(PrescriptionItem item) =>
        item.Category == PrescriptionItemCategory.Drug
            ? TaminPrescriptionItemMetadataReader.ReadString(item, "doseCode", "dose_code")
            : null;

    private static bool IsSnomedDrug(PrescriptionItem item) => ReadDoseCode(item) is not null;

    private static int ToPrescriptionType(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => 1,
        PrescriptionItemCategory.Visit => 3,
        PrescriptionItemCategory.MedicalService => 5,
        PrescriptionItemCategory.Referral => 7,
        _ => 2
    };

    private static string ToServiceType(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => TaminServiceTypeCodes.Drug,
        PrescriptionItemCategory.Visit => TaminServiceTypeCodes.Visit,
        _ => TaminServiceTypeCodes.MedicalService
    };

    private static long? ParsePositiveLong(string? value) =>
        long.TryParse(value, out long parsed) && parsed > 0 ? parsed : null;

    private static EntitlementResponse ToEntitlementResponse(JsonElement result) => new()
    {
        Status = MapTaminEligibility(GetString(result, "status_code", "statusCode", "status")),
        TrackingCode = GetString(result, "tracking_code", "trackingCode"),
        CoverageIdentifier = GetString(result, "insurance_number", "insuranceNumber"),
        RawStatusCode = GetString(result, "status_code", "statusCode", "status"),
        RawStatusMessage = GetString(result, "status_message", "statusMessage", "reason"),
        ExpiresAt = GetDateTime(result, "expires_at", "expiresAt")
    };

    private static SubmissionResponse ToSubmissionResponse(JsonElement result, string? fallbackRemotePrescriptionId = null) => new()
    {
        RemotePrescriptionId = GetString(result, "prescription_id", "prescriptionId", "head_EPRSC_ID") ?? fallbackRemotePrescriptionId,
        RemoteTrackingCode = GetString(result, "tracking_code", "trackingCode"),
        RemoteStatusCode = GetString(result, "status_code", "statusCode", "errorCode"),
        RemoteStatusText = GetString(result, "status_message", "statusMessage", "errorMsg", "reason"),
        CanonicalStatus = MapTaminSubmissionStatus(GetString(result, "status_code", "statusCode", "errorCode"))
    };

    private static FetchResponse ToFetchResponse(JsonElement result) => new()
    {
        RemoteStatusCode = GetString(result, "status_code", "statusCode", "errorCode"),
        RemoteStatusText = GetString(result, "status_message", "statusMessage", "errorMsg", "reason"),
        CanonicalStatus = MapTaminSubmissionStatus(GetString(result, "status_code", "statusCode", "errorCode")),
        RemoteTrackingCode = GetString(result, "tracking_code", "trackingCode")
    };

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            if (element.TryGetProperty(propertyName, out JsonElement property))
            {
                return property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : property.ValueKind == JsonValueKind.Number
                        ? property.GetRawText()
                        : null;
            }
        }

        return null;
    }

    private static DateTime? GetDateTime(JsonElement element, params string[] propertyNames)
    {
        string? value = GetString(element, propertyNames);
        return DateTimeOffset.TryParse(value, out DateTimeOffset parsedOffset)
            ? parsedOffset.UtcDateTime
            : null;
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
        null or "" or "ACCEPTED" or "1" => DomainPrescriptionStatus.AcceptedByProvider,
        "REJECTED" or "0" => DomainPrescriptionStatus.RejectedByProvider,
        "PENDING" or "2" => DomainPrescriptionStatus.Submitted,
        _ => DomainPrescriptionStatus.SyncPending
    };
}
