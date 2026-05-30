using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.TaminInsurance.Client;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

/// <summary>
/// Tamin module adapter. Translates canonical Prescription requests to provider-client requests
/// and coordinates Prescription-owned persistence/session services.
/// </summary>
public sealed class TaminInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "TAMIN";

    private readonly ITaminInsuranceClient _client;
    private readonly ITaminCredentialSessionStore _sessionStore;
    private readonly PrescriptionDbContext _dbContext;
    private readonly TaminErrorMapper _errorMapper;
    private readonly ILogger<TaminInsuranceAdapter> _logger;

    private static readonly HashSet<InsuranceProviderCapabilityType> SupportedCapabilities =
    [
        InsuranceProviderCapabilityType.SupportsEntitlementCheck,
        InsuranceProviderCapabilityType.SupportsVisitPrescription,
        InsuranceProviderCapabilityType.SupportsDrugPrescription,
        InsuranceProviderCapabilityType.SupportsParaclinicPrescription,
        InsuranceProviderCapabilityType.SupportsMedicalServicePrescription,
        InsuranceProviderCapabilityType.SupportsFetchPrescription,
        InsuranceProviderCapabilityType.SupportsEditPrescription,
        InsuranceProviderCapabilityType.SupportsCancelPrescription,
        InsuranceProviderCapabilityType.SupportsReferenceDrugSync,
        InsuranceProviderCapabilityType.SupportsReferenceServiceSync,
        InsuranceProviderCapabilityType.SupportsProviderSession,
    ];

    public TaminInsuranceAdapter(
        ITaminInsuranceClient client,
        ITaminCredentialSessionStore sessionStore,
        PrescriptionDbContext dbContext,
        TaminErrorMapper errorMapper,
        ILogger<TaminInsuranceAdapter> logger)
    {
        _client = client;
        _sessionStore = sessionStore;
        _dbContext = dbContext;
        _errorMapper = errorMapper;
        _logger = logger;
    }


    public string ProviderKey => ProviderKeyConst;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => SupportedCapabilities;

    public async Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateDoctorContext(request.DoctorId, request.ClinicId);
        if (validationError is not null)
            return AdapterResult.Failure(validationError);

        var sessionResult = await _sessionStore.AuthenticateAsync(request.DoctorId, request.ClinicId, cancellationToken);
        return sessionResult.IsSuccess ? AdapterResult.Success() : AdapterResult.Failure(sessionResult.Error!);
    }

    public async Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientNationalId))
            return AdapterResult<EntitlementResponse>.Failure(ValidationError("کد ملی بیمار مشخص نشده است.", "PatientNationalId is required."));

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(request.DoctorId, request.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<EntitlementResponse>.Failure(sessionResult.Error!);

        try
        {
            var result = await _client.CheckEntitlementAsync(
                sessionResult.Session!.Token,
                new TaminEntitlementRequest { NationalCode = request.PatientNationalId },
                cancellationToken);

            return AdapterResult<EntitlementResponse>.Success(TaminPrescriptionMapper.ToEntitlementResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin entitlement check failed for patient {NationalId}", request.PatientNationalId);
            return AdapterResult<EntitlementResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PrescriptionId == Guid.Empty)
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("شناسه نسخه مشخص نشده است.", "PrescriptionId is required."));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("کلید یکتایی درخواست مشخص نشده است.", "IdempotencyKey is required."));

        var prescription = await LoadPrescriptionWithItemsAsync(request.PrescriptionId, cancellationToken);
        if (prescription is null)
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("نسخه مورد نظر یافت نشد.", $"Prescription {request.PrescriptionId} not found."));

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);

        try
        {
            var result = await _client.SubmitPrescriptionAsync(
                sessionResult.Session!.Token,
                TaminPrescriptionMapper.ToTaminSubmission(prescription),
                request.IdempotencyKey,
                cancellationToken);

            return AdapterResult<SubmissionResponse>.Success(TaminPrescriptionMapper.ToSubmissionResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin prescription submission failed for {PrescriptionId}", request.PrescriptionId);
            return AdapterResult<SubmissionResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RemotePrescriptionId))
            return AdapterResult<FetchResponse>.Failure(ValidationError("شناسه نسخه ریموت مشخص نشده است.", "RemotePrescriptionId is required for fetch."));

        var prescription = await LoadPrescriptionByRemoteIdAsync(request.RemotePrescriptionId, includeItems: false, cancellationToken);
        if (prescription is null)
            return AdapterResult<FetchResponse>.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<FetchResponse>.Failure(sessionResult.Error!);

        try
        {
            var result = await _client.FetchPrescriptionAsync(sessionResult.Session!.Token, request.RemotePrescriptionId, cancellationToken);
            return AdapterResult<FetchResponse>.Success(TaminPrescriptionMapper.ToFetchResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin fetch failed for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult<FetchResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PrescriptionId == Guid.Empty)
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("شناسه نسخه مشخص نشده است.", "PrescriptionId is required."));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("کلید یکتایی درخواست مشخص نشده است.", "IdempotencyKey is required."));

        var prescription = await LoadPrescriptionWithItemsAsync(request.PrescriptionId, cancellationToken);
        if (prescription is null)
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("نسخه مورد نظر یافت نشد.", $"Prescription {request.PrescriptionId} not found."));

        var remotePrescriptionId = await _dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => s.RemotePrescriptionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(remotePrescriptionId))
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "نسخه هنوز در سامانه ثبت نشده و قابل ویرایش نیست.",
                IsRetryable = false
            });

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);

        try
        {
            var result = await _client.EditPrescriptionAsync(
                sessionResult.Session!.Token,
                remotePrescriptionId,
                TaminPrescriptionMapper.ToTaminSubmission(prescription),
                request.IdempotencyKey,
                cancellationToken);

            return AdapterResult<SubmissionResponse>.Success(TaminPrescriptionMapper.ToSubmissionResponse(result, remotePrescriptionId));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin edit failed for {PrescriptionId}", request.PrescriptionId);
            return AdapterResult<SubmissionResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RemotePrescriptionId))
            return AdapterResult.Failure(ValidationError("شناسه نسخه ریموت مشخص نشده است.", "RemotePrescriptionId is required."));

        var prescription = await LoadPrescriptionByRemoteIdAsync(request.RemotePrescriptionId, includeItems: false, cancellationToken);
        if (prescription is null)
            return AdapterResult.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult.Failure(sessionResult.Error!);

        try
        {
            await _client.CancelPrescriptionAsync(sessionResult.Session!.Token, request.RemotePrescriptionId, cancellationToken);
            return AdapterResult.Success();
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin cancel failed for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CodeSystem))
            return AdapterResult<ReferenceDataResponse>.Failure(ValidationError("سامانه کد داده مرجع مشخص نشده است.", "CodeSystem is required."));

        try
        {
            var result = await _client.FetchReferenceDataAsync(request.CodeSystem, request.SinceVersion, cancellationToken);
            return AdapterResult<ReferenceDataResponse>.Success(TaminPrescriptionMapper.ToReferenceDataResponse(request, result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "Tamin reference-data fetch failed for {CodeSystem}", request.CodeSystem);
            return AdapterResult<ReferenceDataResponse>.Failure(MapProviderException(ex));
        }
    }

    private Task<Domain.Entities.PrescriptionRecord?> LoadPrescriptionWithItemsAsync(Guid prescriptionId, CancellationToken cancellationToken) =>
        _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

    private async Task<Domain.Entities.PrescriptionRecord?> LoadPrescriptionByRemoteIdAsync(string remotePrescriptionId, bool includeItems, CancellationToken cancellationToken)
    {
        var query =
            from submission in _dbContext.PrescriptionSubmissions
            join prescription in _dbContext.Prescriptions on submission.PrescriptionRecordId equals prescription.Id
            where submission.RemotePrescriptionId == remotePrescriptionId
            select prescription;

        if (includeItems)
            query = query.Include(p => p.Items);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private static IntegrationError? ValidateDoctorContext(Guid doctorId, Guid clinicId)
    {
        if (doctorId == Guid.Empty)
            return ValidationError("شناسه پزشک مشخص نشده است.", "DoctorId is required.");
        if (clinicId == Guid.Empty)
            return ValidationError("شناسه کلینیک مشخص نشده است.", "ClinicId is required.");
        return null;
    }

    private IntegrationError MapProviderException(Exception exception)
    {
        if (exception is TaminInsuranceClientException clientException)
        {
            return _errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = (int)clientException.StatusCode,
                ProviderErrorCode = ((int)clientException.StatusCode).ToString(),
                ProviderErrorMessage = clientException.ResponseBody
            });
        }

        return _errorMapper.MapError(new ProviderErrorContext
        {
            Exception = exception,
            ProviderErrorMessage = exception.Message
        });
    }

    private static bool IsProviderException(Exception exception) =>
        exception is TaminInsuranceClientException or HttpRequestException or TaskCanceledException;

    private static IntegrationError ValidationError(string userMessage, string technicalDetail) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = userMessage,
        TechnicalDetail = technicalDetail,
        IsRetryable = false
    };
}
