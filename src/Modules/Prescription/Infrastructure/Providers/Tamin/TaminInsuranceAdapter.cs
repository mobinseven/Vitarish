using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;
using Vorn.Tamin;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

/// <summary>
/// Tamin module adapter. Translates canonical Prescription requests to provider-client requests
/// and coordinates Prescription-owned persistence/session services.
/// </summary>
public sealed class TaminInsuranceAdapter(
    ITaminCredentialSessionStore sessionStore,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<TaminOptions> taminOptions,
    PrescriptionDbContext dbContext,
    TaminErrorMapper errorMapper,
    ILogger<TaminInsuranceAdapter> logger) : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "TAMIN";

    private readonly ITaminCredentialSessionStore _sessionStore = sessionStore;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IOptionsMonitor<TaminOptions> _taminOptions = taminOptions;
    private readonly PrescriptionDbContext _dbContext = dbContext;
    private readonly TaminErrorMapper _errorMapper = errorMapper;
    private readonly ILogger<TaminInsuranceAdapter> _logger = logger;

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
            var result = await sessionResult.Session!.Identity.CheckEntitlementAsync(
                new CheckEntitlementRequest
                {
                    NationalId = request.PatientNationalId,
                    ProviderId = sessionResult.DoctorTaminId ?? sessionResult.Session.ClientId ?? string.Empty,
                    VisitDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
                    ServiceType = "clinic"
                }, cancellationToken);

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
        var distinctCategories = prescription.Items
            .Select(i => i.Category)
            .Distinct()
            .Take(2)
            .ToList();
        if (distinctCategories.Count > 1)
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "ارسال نسخه با اقلام ترکیبی در تامین اجتماعی پشتیبانی نمی‌شود.",
                TechnicalDetail = $"Mixed prescription categories are not supported. Found categories: {string.Join(", ", distinctCategories)}.",
                IsRetryable = false
            });

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);

        try
        {
            var result = await TaminPrescriptionMapper.RegisterPrescriptionAsync(
                sessionResult.Session!,
                prescription,
                sessionResult.DoctorTaminId ?? string.Empty,
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

        var (prescription, submission) = await LoadPrescriptionAndSubmissionByRemoteIdAsync(request.RemotePrescriptionId, cancellationToken);
        if (prescription is null)
            return AdapterResult<FetchResponse>.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));

        if (string.IsNullOrWhiteSpace(submission?.RemoteTrackingCode))
            return AdapterResult<FetchResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for remote prescription {request.RemotePrescriptionId}.",
                IsRetryable = false
            });

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<FetchResponse>.Failure(sessionResult.Error!);

        try
        {
            var patientNationalId = prescription.PatientNationalId ?? prescription.PatientId.ToString();
            var result = await sessionResult.Session!.Prescription.GetRegisteredPrescriptionAsync(
                request.RemotePrescriptionId,
                submission.RemoteTrackingCode,
                patientNationalId,
                cancellationToken);

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

        var submission = await _dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(submission?.RemotePrescriptionId))
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "نسخه هنوز در سامانه ثبت نشده و قابل ویرایش نیست.",
                IsRetryable = false
            });

        if (string.IsNullOrWhiteSpace(submission.RemoteTrackingCode))
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست و ویرایش امکان‌پذیر نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for submission of prescription {request.PrescriptionId}.",
                IsRetryable = false
            });

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);

        try
        {
            var editedItems = prescription.Items
                .Select(i => (object)new { service_code = i.ReferenceCode, quantity = i.Quantity })
                .ToList();

            var result = await sessionResult.Session!.Prescription.EditElectronicPrescriptionAsync(
                new EditPrescriptionRequest
                {
                    PrescriptionId = submission.RemotePrescriptionId,
                    TrackingCode = submission.RemoteTrackingCode,
                    EditedItems = editedItems,
                    EditReason = "ویرایش نسخه"
                }, cancellationToken);

            return AdapterResult<SubmissionResponse>.Success(
                TaminPrescriptionMapper.ToSubmissionResponse(result, submission.RemotePrescriptionId));
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

        var (prescription, submission) = await LoadPrescriptionAndSubmissionByRemoteIdAsync(request.RemotePrescriptionId, cancellationToken);
        if (prescription is null)
            return AdapterResult.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));

        if (string.IsNullOrWhiteSpace(submission?.RemoteTrackingCode))
            return AdapterResult.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست و لغو امکان‌پذیر نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for remote prescription {request.RemotePrescriptionId}.",
                IsRetryable = false
            });

        var sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
            return AdapterResult.Failure(sessionResult.Error!);

        try
        {
            await sessionResult.Session!.Prescription.DeleteElectronicPrescriptionAsync(
                new DeletePrescriptionRequest
                {
                    PrescriptionId = request.RemotePrescriptionId,
                    TrackingCode = submission.RemoteTrackingCode,
                    DeleteReason = "لغو نسخه"
                }, cancellationToken);

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

        var options = _taminOptions.CurrentValue;
        var httpClient = _httpClientFactory.CreateClient("TaminClient");
        var session = new TaminSession(httpClient, baseUri: new Uri(options.EffectiveBaseUrl), needToken: false, clientId: options.ApiKey);

        try
        {
            var result = request.CodeSystem.Equals("drug", StringComparison.OrdinalIgnoreCase)
                ? await session.Service.GetDrugListAsync(cancellationToken: cancellationToken)
                : await session.Service.GetServiceListAsync(cancellationToken: cancellationToken);

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

    private async Task<(Domain.Entities.PrescriptionRecord? Prescription, Domain.Entities.PrescriptionProviderSubmission? Submission)>
        LoadPrescriptionAndSubmissionByRemoteIdAsync(string remotePrescriptionId, CancellationToken cancellationToken)
    {
        var row = await (
            from submission in _dbContext.PrescriptionSubmissions
            join prescription in _dbContext.Prescriptions on submission.PrescriptionRecordId equals prescription.Id
            where submission.RemotePrescriptionId == remotePrescriptionId
            orderby submission.SubmittedAt descending
            select new { prescription, submission }
        ).FirstOrDefaultAsync(cancellationToken);

        return (row?.prescription, row?.submission);
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
        if (exception is ConnectionError connectionError)
        {
            return _errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = connectionError.StatusCode.HasValue ? (int)connectionError.StatusCode.Value : null,
                ProviderErrorCode = connectionError.StatusCode.HasValue ? ((int)connectionError.StatusCode.Value).ToString() : null,
                ProviderErrorMessage = connectionError.Content
            });
        }

        if (exception is UserLoginException loginException)
        {
            return _errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = loginException.Status,
                ProviderErrorCode = loginException.Status?.ToString(),
                ProviderErrorMessage = loginException.ReasonText ?? loginException.Message
            });
        }

        return _errorMapper.MapError(new ProviderErrorContext
        {
            Exception = exception,
            ProviderErrorMessage = exception.Message
        });
    }

    private static bool IsProviderException(Exception exception) =>
        exception is ConnectionError or UserLoginException or HttpRequestException or TaskCanceledException;

    private static IntegrationError ValidationError(string userMessage, string technicalDetail) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = userMessage,
        TechnicalDetail = technicalDetail,
        IsRetryable = false
    };
}
