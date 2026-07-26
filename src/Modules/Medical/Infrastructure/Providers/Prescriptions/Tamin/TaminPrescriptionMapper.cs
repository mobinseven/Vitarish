using System.Text.Json;
using Tamin.Integration.Mapping;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using DomainPrescriptionStatus = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionStatus;
using EligibilityStatus = Vitarish.Medical.Domain.Enums.Prescriptions.EligibilityStatus;
using PrescriptionItemCategory = Vitarish.Medical.Domain.Enums.Prescriptions.PrescriptionItemCategory;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminPrescriptionMapper(
    PrescriptionRequestMapper requestMapper,
    TaminMappingService responseMapper)
{
    public async Task<SubmissionResponse> RegisterPrescriptionAsync(
        TaminProviderSession session,
        PrescriptionRecord prescription,
        string doctorTaminId,
        string doctorNationalCode,
        string? clinicSiamId,
        string mobile,
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

        PrescriptionMutationResult result;
        if (session.Production is { } production)
        {
            var request = requestMapper.MapProduction(input);
            ApplyProductionServiceTypes(request.NoteDetailEprscs, prescription.Items);
            var response = await production.Soa.Interface.Epresc.SendEpresc.V7.PostAsync(
                request,
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin create-prescription response was empty.");
            result = responseMapper.MapCreatePrescription(response);
        }
        else
        {
            var request = requestMapper.MapPilot(input);
            ApplyPilotServiceTypes(request.NoteDetailEprscs, prescription.Items);
            var response = await session.Pilot!.Soa.Api.V2.SendEpresc.PostAsync(
                request,
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin pilot create-prescription response was empty.");
            result = responseMapper.MapCreatePrescription(response);
        }

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

    public async Task<EntitlementResponse> CheckEntitlementAsync(
        TaminProviderSession session,
        string siamId,
        string doctorId,
        string doctorNationalCode,
        string patientNationalId,
        CancellationToken cancellationToken)
    {
        JsonElement payload;
        if (session.Production is { } production)
        {
            var response = await production.Soa.Interface.Epresc.Patient.V2.DeserveInfo[siamId][doctorId][patientNationalId]
                .GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin entitlement response was empty.");
            payload = responseMapper.MapPatientEntitlement(response).Payload;
        }
        else
        {
            var response = await session.Pilot!.Soa.Api.V2.Patients.DeserveInfo[doctorNationalCode][siamId][doctorId][patientNationalId]
                .GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin pilot entitlement response was empty.");
            payload = responseMapper.MapPilotUndocumentedMutation(response).Payload;
        }

        return ToEntitlementResponse(payload);
    }

    public async Task<FetchResponse> FetchPrescriptionAsync(
        TaminProviderSession session,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        CancellationToken cancellationToken)
    {
        JsonElement payload;
        if (session.Production is { } production)
        {
            var response = await production.Soa.Interface.Epresc.SendEpresc.V2[remotePrescriptionId][doctorId]
                .GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin prescription-details response was empty.");
            payload = responseMapper.MapPrescriptionDetails(response).Payload;
        }
        else
        {
            var response = await session.Pilot!.Soa.Api.V2.Ep[remotePrescriptionId][doctorNationalCode][doctorId]
                .Detail.GetAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin pilot prescription-details response was empty.");
            payload = responseMapper.MapPilotUndocumentedMutation(response).Payload;
        }

        return ToFetchResponse(payload);
    }

    public async Task<SubmissionResponse> EditPrescriptionAsync(
        TaminProviderSession session,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        IEnumerable<PrescriptionItem> items,
        CancellationToken cancellationToken)
    {
        JsonElement payload;
        if (session.Production is { } production)
        {
            var body = items.Select(ToProductionDetail).ToList();
            var response = await production.Soa.Interface.Epresc.SendEpresc.V2.Edit[remotePrescriptionId.ToString()][doctorId]
                .PostAsync(body, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin edit-prescription response was empty.");
            payload = responseMapper.MapUpdatePrescription(response).Payload;
        }
        else
        {
            var body = items.Select(ToPilotDetail).ToList();
            var response = await session.Pilot!.Soa.Api.V2.Ep.Update[remotePrescriptionId][doctorNationalCode][doctorId]
                .PostAsync(body, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin pilot edit-prescription response was empty.");
            payload = responseMapper.MapPilotUndocumentedMutation(response).Payload;
        }

        return ToSubmissionResponse(payload, remotePrescriptionId.ToString());
    }

    public async Task CancelPrescriptionAsync(
        TaminProviderSession session,
        long remotePrescriptionId,
        string doctorId,
        string doctorNationalCode,
        CancellationToken cancellationToken)
    {
        if (session.Production is { } production)
        {
            _ = await production.Soa.Interface.Epresc.SendEpresc.V2.Remove[remotePrescriptionId.ToString()][doctorId]
                .PostAsync(cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Tamin delete-prescription response was empty.");
            return;
        }

        _ = await session.Pilot!.Soa.Api.V2.Ep[remotePrescriptionId][doctorNationalCode][doctorId]
            .PostAsync(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Tamin pilot delete-prescription response was empty.");
    }

    private static PrescriptionCreateInput CreateInput(
        PrescriptionRecord prescription,
        string doctorTaminId,
        string doctorNationalCode,
        string? clinicSiamId,
        string mobile,
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
            Complaint: null,
            CreatorType: null,
            DocId: doctorTaminId,
            DocMobileNo: null,
            DocNationalCode: doctorNationalCode,
            ExpireDate: null,
            Mobile: mobile,
            NoteDetailEprscs: details,
            NoteDetailsReferralList: referrals,
            Patient: patientNationalId,
            PrescDate: prescriptionDate,
            PrescType: new PrescriptionTypeInput(ToPrescriptionType(category)),
            ReferralFeedbackId: null);
    }

    private static PrescriptionDetailInput ToDetailInput(PrescriptionItem item) => new(
        DateDo: null,
        DiagnosisID: null,
        Dose: item.DosageJson,
        DrugInstruction: null,
        IllnessId: TaminPrescriptionItemMetadataReader.ReadString(item, "illnessId", "illness_id"),
        IsDentalService: item.Category == PrescriptionItemCategory.Dental
            ? DentalServiceFlag.One
            : DentalServiceFlag.Zero,
        NoteDetailDrug: null,
        NoteDetailsEprscId: ParsePositiveLong(item.ProviderItemIdentifier),
        PlanId: TaminPrescriptionItemMetadataReader.ReadString(item, "planId", "plan_id"),
        ReferenceStatus: null,
        Repeat: item.DurationDays?.ToString(),
        SrvId: new ServiceInput(item.ReferenceCode, null),
        SrvQty: item.Quantity,
        TimesAday: null,
        ToothId: TaminPrescriptionItemMetadataReader.ReadString(item, "toothId", "tooth_id"));

    private static global::Tamin.Client.Soa.Prod.Models.NoteDetailEprsc ToProductionDetail(PrescriptionItem item)
    {
        PrescriptionDetailInput input = ToDetailInput(item);
        return new()
        {
            DateDo = input.DateDo,
            Dose = input.Dose,
            IllnessId = input.IllnessId,
            IsDentalService = input.IsDentalService == DentalServiceFlag.One
                ? global::Tamin.Client.Soa.Prod.Models.NoteDetailEprsc_isDentalService.One
                : global::Tamin.Client.Soa.Prod.Models.NoteDetailEprsc_isDentalService.Zero,
            NoteDetailsEprscId = input.NoteDetailsEprscId,
            PlanId = input.PlanId,
            Repeat = input.Repeat,
            SrvId = new global::Tamin.Client.Soa.Prod.Models.Service
            {
                SrvCode = input.SrvId?.SrvCode,
                SrvType = new() { SrvType = ToProductionServiceType(item.Category) }
            },
            SrvQty = input.SrvQty,
            ToothId = input.ToothId
        };
    }

    private static global::Tamin.Client.Soa.Pilot.Models.NoteDetailEprsc ToPilotDetail(PrescriptionItem item)
    {
        PrescriptionDetailInput input = ToDetailInput(item);
        return new()
        {
            DateDo = input.DateDo,
            Dose = input.Dose,
            IllnessId = input.IllnessId,
            IsDentalService = input.IsDentalService == DentalServiceFlag.One
                ? global::Tamin.Client.Soa.Pilot.Models.NoteDetailEprsc_isDentalService.One
                : global::Tamin.Client.Soa.Pilot.Models.NoteDetailEprsc_isDentalService.Zero,
            NoteDetailsEprscId = input.NoteDetailsEprscId,
            PlanId = input.PlanId,
            Repeat = input.Repeat,
            SrvId = new global::Tamin.Client.Soa.Pilot.Models.Service
            {
                SrvCode = input.SrvId?.SrvCode,
                SrvType = new() { SrvType = ToPilotServiceType(item.Category) }
            },
            SrvQty = input.SrvQty,
            ToothId = input.ToothId
        };
    }

    private static int ToPrescriptionType(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => 1,
        PrescriptionItemCategory.Visit => 3,
        PrescriptionItemCategory.MedicalService => 5,
        PrescriptionItemCategory.Referral => 7,
        _ => 2
    };

    private static void ApplyProductionServiceTypes(
        IReadOnlyList<global::Tamin.Client.Soa.Prod.Models.NoteDetailEprsc>? details,
        IEnumerable<PrescriptionItem> items)
    {
        if (details is null)
        {
            return;
        }

        foreach ((global::Tamin.Client.Soa.Prod.Models.NoteDetailEprsc detail, PrescriptionItem item) in
                 details.Zip(items.OrderBy(item => item.SortOrder)))
        {
            detail.SrvId ??= new();
            detail.SrvId.SrvType = new() { SrvType = ToProductionServiceType(item.Category) };
        }
    }

    private static void ApplyPilotServiceTypes(
        IReadOnlyList<global::Tamin.Client.Soa.Pilot.Models.NoteDetailEprsc>? details,
        IEnumerable<PrescriptionItem> items)
    {
        if (details is null)
        {
            return;
        }

        foreach ((global::Tamin.Client.Soa.Pilot.Models.NoteDetailEprsc detail, PrescriptionItem item) in
                 details.Zip(items.OrderBy(item => item.SortOrder)))
        {
            detail.SrvId ??= new();
            detail.SrvId.SrvType = new() { SrvType = ToPilotServiceType(item.Category) };
        }
    }

    private static global::Tamin.Client.Soa.Prod.Models.ServiceType_srvType ToProductionServiceType(
        PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => global::Tamin.Client.Soa.Prod.Models.ServiceType_srvType.ZeroOne,
        PrescriptionItemCategory.Visit => global::Tamin.Client.Soa.Prod.Models.ServiceType_srvType.OneSix,
        _ => global::Tamin.Client.Soa.Prod.Models.ServiceType_srvType.OneSeven
    };

    private static global::Tamin.Client.Soa.Pilot.Models.ServiceType_srvType ToPilotServiceType(
        PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => global::Tamin.Client.Soa.Pilot.Models.ServiceType_srvType.ZeroOne,
        PrescriptionItemCategory.Visit => global::Tamin.Client.Soa.Pilot.Models.ServiceType_srvType.OneSix,
        _ => global::Tamin.Client.Soa.Pilot.Models.ServiceType_srvType.OneSeven
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
