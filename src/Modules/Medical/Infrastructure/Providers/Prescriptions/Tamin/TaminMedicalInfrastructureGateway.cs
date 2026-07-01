using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Vitarish.Modules.Base.Core.Platform.Errors;
using Vorn.Tamin;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed class TaminMedicalInfrastructureGateway(
    ITaminSessionFactory sessionFactory,
    ITaminCredentialSessionStore sessionStore,
    IOptionsMonitor<TaminOptions> optionsMonitor) : ITaminMedicalInfrastructureGateway
{
    private readonly ITaminSessionFactory _sessionFactory = sessionFactory;
    private readonly ITaminCredentialSessionStore _sessionStore = sessionStore;
    private readonly IOptionsMonitor<TaminOptions> _optionsMonitor = optionsMonitor;

    public async Task<TaminReferencePageResult> SearchDrugsAsync(TaminDrugSearchOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetDrugListAsync(
            request.SearchText,
            request.DrugCode,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize),
            request.ActiveOnly,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.Drug, result, request.Page, request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(
        TaminDrugListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int page = 1;
        int retrieved = 0;
        int maxPages = request.MaxPages ?? _optionsMonitor.CurrentValue.MaxReferencePages;
        while (page <= maxPages)
        {
            TaminReferencePageResult result = await SearchDrugsAsync(new TaminDrugSearchOperationRequest(request.SearchText, request.DrugCode, page, request.PageSize, request.ActiveOnly), cancellationToken);
            yield return result;
            retrieved += result.Items.Count;
            if (ShouldStop(result, retrieved, request.PageSize)) yield break;
            page++;
        }

        throw new InvalidOperationException($"Tamin drug reference-data sync exceeded {maxPages} pages before completion.");
    }

    public async Task<TaminReferencePageResult> SearchServicesAsync(TaminServiceSearchOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetServiceListAsync(
            request.ServiceType,
            request.ServiceGroup,
            request.SearchText,
            NormalizePage(request.Page),
            NormalizePageSize(request.PageSize),
            request.ActiveOnly,
            cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.Service, result, request.Page, request.PageSize);
    }

    public async IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(
        TaminServiceListAllOperationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int page = 1;
        int retrieved = 0;
        int maxPages = request.MaxPages ?? _optionsMonitor.CurrentValue.MaxReferencePages;
        while (page <= maxPages)
        {
            TaminReferencePageResult result = await SearchServicesAsync(new TaminServiceSearchOperationRequest(request.ServiceType, request.ServiceGroup, request.SearchText, page, request.PageSize, request.ActiveOnly), cancellationToken);
            yield return result;
            retrieved += result.Items.Count;
            if (ShouldStop(result, retrieved, request.PageSize)) yield break;
            page++;
        }

        throw new InvalidOperationException($"Tamin service reference-data sync exceeded {maxPages} pages before completion.");
    }

    public async Task<TaminReferencePageResult> GetAllServicesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetAllServicesAsync(request.Query, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.AllServices, result, null, null);
    }

    public async Task<TaminReferencePageResult> GetPrescriptionTypesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetPrescriptionTypeAsync(request.Query, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.PrescriptionType, result, null, null);
    }

    public async Task<TaminReferencePageResult> GetParaclinicTariffsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetParaclinicTarefAsync(request.Query, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.ParaclinicTariff, result, null, null);
    }

    public async Task<TaminReferencePageResult> GetDrugAmountsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetDrugAmountAsync(request.Query, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.DrugAmount, result, null, null);
    }

    public async Task<TaminReferencePageResult> GetDrugInstructionsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken)
    {
        JsonElement result = await _sessionFactory.CreateReferenceDataSession().Service.GetDrugInstructionAsync(request.Query, cancellationToken);
        return TaminProviderJsonReader.ToReferencePage(TaminProviderOperationNames.DrugInstruction, result, null, null);
    }


    public async Task<TaminProviderJsonResult> CheckPrescriptionWarningAsync(TaminWarningOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.PrescriptionWarning, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.PrescriptionWarning, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Prescription.CheckPrescriptionWarningAsync(
            new CheckWarningRequest
            {
                DoctorId = sessionResult.DoctorTaminId ?? string.Empty,
                PatientNationalId = request.PatientNationalId,
                PrescriptionItems = request.PrescriptionItems
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.PrescriptionWarning, result);
    }

    public async Task<TaminProviderJsonResult> CheckDentalRulesAsync(TaminWarningOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.DentalRules, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.DentalRules, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Dentistry.CheckRulesAsync(
            new CheckWarningRequest
            {
                DoctorId = sessionResult.DoctorTaminId ?? string.Empty,
                PatientNationalId = request.PatientNationalId,
                PrescriptionItems = request.PrescriptionItems
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.DentalRules, result);
    }

    public async Task<TaminProviderJsonResult> GetReferralCountsAsync(TaminReferralCountOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.ReferralCounts, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.ReferralCounts, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Referrals.GetCountsAsync(
            new ReferralCountRequest
            {
                PatientNationalCode = request.PatientNationalCode,
                DoctorId = string.IsNullOrWhiteSpace(request.TaminDoctorId) ? sessionResult.DoctorTaminId ?? string.Empty : request.TaminDoctorId
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.ReferralCounts, result);
    }

    public async Task<TaminProviderJsonResult> RecordReferralFeedbackAsync(TaminReferralFeedbackOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.ReferralFeedback, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.ReferralFeedback, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Referrals.RecordFeedbackAsync(
            new ReferralFeedbackRequest
            {
                ReferralId = request.ReferralId,
                Message = request.Message
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.ReferralFeedback, result);
    }

    public async Task<TaminProviderJsonResult> GetNurseTodoListAsync(TaminNurseTodoListOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.NurseTodoList, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.NurseTodoList, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Nurse.GetTodoListAsync(
            new NurseTodoListRequest
            {
                SiamId = request.SiamId,
                PatientNationalCode = request.PatientNationalCode
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.NurseTodoList, result);
    }

    public async Task<TaminProviderJsonResult> RecordNurseActionAsync(TaminNurseActionOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.NurseAction, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.NurseAction, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Nurse.RecordActionAsync(
            new NurseActionWorkflowRequest
            {
                SiamId = request.SiamId,
                NurseNationalCode = request.NurseNationalCode,
                PrescriptionDetailIds = request.PrescriptionDetailIds
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.NurseAction, result);
    }

    public async Task<TaminProviderJsonResult> CreateHospitalizationAsync(TaminHospitalizationCreateOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.HospitalizationCreate, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.HospitalizationCreate, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Hospitalization.CreateAsync(
            new HospitalizationCreateRequest
            {
                ReferralId = request.ReferralId,
                SiamId = request.SiamId
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.HospitalizationCreate, result);
    }

    public async Task<TaminProviderJsonResult> GetHospitalizationSecretaryListAsync(TaminHospitalizationSecretaryListOperationRequest request, CancellationToken cancellationToken)
    {
        TaminSessionResult sessionResult = await GetDoctorSessionAsync(request.DoctorId, request.ClinicId, TaminProviderOperationNames.HospitalizationSecretaryList, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return TaminProviderJsonResult.Failure(TaminProviderOperationNames.HospitalizationSecretaryList, sessionResult.Error!);
        }

        JsonElement result = await sessionResult.Session!.Hospitalization.GetSecretaryListAsync(
            new HospitalizationSecretaryListRequest
            {
                SiamId = request.SiamId,
                SecretaryNationalCode = request.SecretaryNationalCode
            }, cancellationToken);
        return ToJsonResult(TaminProviderOperationNames.HospitalizationSecretaryList, result);
    }

    private int NormalizePage(int page) => Math.Max(1, page);
    private int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, Math.Max(1, _optionsMonitor.CurrentValue.MaxPageSize));

    private static bool ShouldStop(TaminReferencePageResult result, int retrieved, int requestedPageSize)
    {
        if (result.Items.Count == 0) return true;
        if (result.HasNextPage == false) return true;
        if (result.TotalCount.HasValue && retrieved >= result.TotalCount.Value) return true;
        return !result.HasNextPage.HasValue && result.Items.Count < requestedPageSize;
    }

    private async Task<TaminSessionResult> GetDoctorSessionAsync(Guid doctorId, Guid clinicId, string operationName, CancellationToken cancellationToken)
    {
        if (doctorId == Guid.Empty)
        {
            return TaminSessionResult.Failure(ValidationError(operationName, "DoctorId is required."));
        }

        if (clinicId == Guid.Empty)
        {
            return TaminSessionResult.Failure(ValidationError(operationName, "ClinicId is required."));
        }

        return await _sessionStore.GetOrRefreshSessionAsync(doctorId, clinicId, cancellationToken);
    }

    private static TaminProviderJsonResult ToJsonResult(string operationName, JsonElement result) =>
        TaminProviderJsonResult.Success(operationName, result.GetRawText());

    private static IntegrationError ValidationError(string operationName, string technicalDetail) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = "درخواست ارسالی به تامین اجتماعی معتبر نیست.",
        TechnicalDetail = $"Tamin operation '{operationName}' validation failed. {technicalDetail}",
        IsRetryable = false
    };

}
