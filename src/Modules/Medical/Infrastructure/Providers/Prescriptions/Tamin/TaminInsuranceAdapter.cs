using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Entities;
using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vorn.Tamin;
using Vitarish.Medical.Infrastructure.Data.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

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
        IntegrationError? validationError = ValidateDoctorContext(request.DoctorId, request.ClinicId);
        if (validationError is not null)
        {
            return AdapterResult.Failure(validationError);
        }

        TaminSessionResult sessionResult = await _sessionStore.AuthenticateAsync(request.DoctorId, request.ClinicId, cancellationToken);
        return sessionResult.IsSuccess ? AdapterResult.Success() : AdapterResult.Failure(sessionResult.Error!);
    }

    public async Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientNationalId))
        {
            return AdapterResult<EntitlementResponse>.Failure(ValidationError("کد ملی بیمار مشخص نشده است.", "PatientNationalId is required."));
        }

        TaminSessionResult sessionResult = await _sessionStore.GetOrRefreshSessionAsync(request.DoctorId, request.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return AdapterResult<EntitlementResponse>.Failure(sessionResult.Error!);
        }

        if (string.IsNullOrWhiteSpace(sessionResult.ClinicSiamId))
        {
            return AdapterResult<EntitlementResponse>.Failure(MissingClinicSiamIdError(request.ClinicId));
        }

        try
        {
            JsonElement result = await sessionResult.Session!.Eligibility.LookupPrivatePracticeAsync(
                new EligibilityLookupRequest
                {
                    SiamId = sessionResult.ClinicSiamId,
                    DoctorId = sessionResult.DoctorTaminId ?? string.Empty,
                    DoctorNationalCode = sessionResult.DoctorNationalCode ?? sessionResult.DoctorTaminId ?? string.Empty,
                    PatientNationalCode = request.PatientNationalId
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
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("شناسه نسخه مشخص نشده است.", "PrescriptionId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("کلید یکتایی درخواست مشخص نشده است.", "IdempotencyKey is required."));
        }

        PrescriptionRecord? prescription = await LoadPrescriptionWithItemsAsync(request.PrescriptionId, cancellationToken);
        if (prescription is null)
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("نسخه مورد نظر یافت نشد.", $"Prescription {request.PrescriptionId} not found."));
        }

        List<PrescriptionItemCategory> distinctCategories = prescription.Items
            .Select(i => i.Category)
            .Distinct()
            .Take(2)
            .ToList();
        if (distinctCategories.Count > 1)
        {
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "ارسال نسخه با اقلام ترکیبی در تامین اجتماعی پشتیبانی نمی‌شود.",
                TechnicalDetail = $"Mixed prescription categories are not supported. Found categories: {string.Join(", ", distinctCategories)}.",
                IsRetryable = false
            });
        }

        TaminSessionResult sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);
        }

        try
        {
            JsonElement result = await TaminPrescriptionMapper.RegisterPrescriptionAsync(
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
        {
            return AdapterResult<FetchResponse>.Failure(ValidationError("شناسه نسخه ریموت مشخص نشده است.", "RemotePrescriptionId is required for fetch."));
        }

        (PrescriptionRecord? prescription, PrescriptionProviderSubmission? submission) = await LoadPrescriptionAndSubmissionByRemoteIdAsync(request.RemotePrescriptionId, cancellationToken);
        if (prescription is null)
        {
            return AdapterResult<FetchResponse>.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));
        }

        if (string.IsNullOrWhiteSpace(submission?.RemoteTrackingCode))
        {
            return AdapterResult<FetchResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for remote prescription {request.RemotePrescriptionId}.",
                IsRetryable = false
            });
        }

        TaminSessionResult sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return AdapterResult<FetchResponse>.Failure(sessionResult.Error!);
        }

        try
        {
            if (!TryParseHeaderId(request.RemotePrescriptionId, out int headerId))
            {
                return AdapterResult<FetchResponse>.Failure(InvalidHeaderIdError(request.RemotePrescriptionId));
            }

            string patientNationalId = prescription.PatientNationalId ?? prescription.PatientId.ToString();
            JsonElement result = await sessionResult.Session!.Prescription.GetRegisteredPrescriptionAsync(
                headerId,
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
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("شناسه نسخه مشخص نشده است.", "PrescriptionId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("کلید یکتایی درخواست مشخص نشده است.", "IdempotencyKey is required."));
        }

        PrescriptionRecord? prescription = await LoadPrescriptionWithItemsAsync(request.PrescriptionId, cancellationToken);
        if (prescription is null)
        {
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("نسخه مورد نظر یافت نشد.", $"Prescription {request.PrescriptionId} not found."));
        }

        PrescriptionProviderSubmission? submission = await _dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(submission?.RemotePrescriptionId))
        {
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "نسخه هنوز در سامانه ثبت نشده و قابل ویرایش نیست.",
                IsRetryable = false
            });
        }

        if (string.IsNullOrWhiteSpace(submission.RemoteTrackingCode))
        {
            return AdapterResult<SubmissionResponse>.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست و ویرایش امکان‌پذیر نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for submission of prescription {request.PrescriptionId}.",
                IsRetryable = false
            });
        }

        TaminSessionResult sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return AdapterResult<SubmissionResponse>.Failure(sessionResult.Error!);
        }

        try
        {
            if (!TryParseHeaderId(submission.RemotePrescriptionId, out int headerId))
            {
                return AdapterResult<SubmissionResponse>.Failure(InvalidHeaderIdError(submission.RemotePrescriptionId));
            }

            IReadOnlyList<object> editedItems = TaminEditedPrescriptionItemMapper.ToEditedItems(prescription.Items);

            JsonElement result = await sessionResult.Session!.Prescription.EditElectronicPrescriptionAsync(
                new EditPrescriptionRequest
                {
                    HeaderId = headerId,
                    DoctorNationalCode = sessionResult.DoctorNationalCode ?? sessionResult.DoctorTaminId ?? string.Empty,
                    DoctorId = sessionResult.DoctorTaminId ?? string.Empty,
                    EditedItems = editedItems
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
        {
            return AdapterResult.Failure(ValidationError("شناسه نسخه ریموت مشخص نشده است.", "RemotePrescriptionId is required."));
        }

        (PrescriptionRecord? prescription, PrescriptionProviderSubmission? submission) = await LoadPrescriptionAndSubmissionByRemoteIdAsync(request.RemotePrescriptionId, cancellationToken);
        if (prescription is null)
        {
            return AdapterResult.Failure(ValidationError("سابقه ثبت نسخه یافت نشد.", $"No prescription found for remote ID {request.RemotePrescriptionId}."));
        }

        if (string.IsNullOrWhiteSpace(submission?.RemoteTrackingCode))
        {
            return AdapterResult.Failure(new IntegrationError
            {
                Category = ErrorCategory.BusinessRule,
                UserMessage = "کد پیگیری نسخه موجود نیست و لغو امکان‌پذیر نیست.",
                TechnicalDetail = $"RemoteTrackingCode not stored for remote prescription {request.RemotePrescriptionId}.",
                IsRetryable = false
            });
        }

        TaminSessionResult sessionResult = await _sessionStore.GetOrRefreshSessionAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (!sessionResult.IsSuccess)
        {
            return AdapterResult.Failure(sessionResult.Error!);
        }

        try
        {
            if (!TryParseHeaderId(request.RemotePrescriptionId, out int headerId))
            {
                return AdapterResult.Failure(InvalidHeaderIdError(request.RemotePrescriptionId));
            }

            _ = await sessionResult.Session!.Prescription.DeleteElectronicPrescriptionAsync(
                new DeletePrescriptionRequest
                {
                    HeaderId = headerId,
                    DoctorNationalCode = sessionResult.DoctorNationalCode ?? sessionResult.DoctorTaminId ?? string.Empty,
                    DoctorId = sessionResult.DoctorTaminId ?? string.Empty
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
        {
            return AdapterResult<ReferenceDataResponse>.Failure(ValidationError("سامانه کد داده مرجع مشخص نشده است.", "CodeSystem is required."));
        }

        TaminOptions options = _taminOptions.CurrentValue;
        HttpClient httpClient = _httpClientFactory.CreateClient("TaminClient");
        TaminSession session = new(httpClient, baseUri: new Uri(options.EffectiveBaseUrl), needToken: false, clientId: options.ApiKey);

        try
        {
            JsonElement result = request.CodeSystem.Equals("drug", StringComparison.OrdinalIgnoreCase)
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

    private static bool TryParseHeaderId(string? remotePrescriptionId, out int headerId) =>
        int.TryParse(remotePrescriptionId, out headerId) && headerId > 0;

    private static IntegrationError InvalidHeaderIdError(string? remotePrescriptionId) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = "شناسه نسخه ریموت معتبر نیست.",
        TechnicalDetail = $"RemotePrescriptionId must be a positive integer header ID for Tamin v2. Value: {remotePrescriptionId}",
        IsRetryable = false
    };

    private static IntegrationError MissingClinicSiamIdError(Guid clinicId) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = "شناسه سیام کلینیک برای تامین اجتماعی ثبت نشده است.",
        TechnicalDetail = $"ProviderClinicIdentifier (SiamId) is required for clinic {clinicId} and provider TAMIN.",
        IsRetryable = false
    };

    private Task<Domain.Entities.Prescriptions.PrescriptionRecord?> LoadPrescriptionWithItemsAsync(Guid prescriptionId, CancellationToken cancellationToken) =>
        _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

    private async Task<(Domain.Entities.Prescriptions.PrescriptionRecord? Prescription, Domain.Entities.Prescriptions.PrescriptionProviderSubmission? Submission)>
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

    private static IntegrationError? ValidateDoctorContext(Guid doctorId, Guid clinicId) => doctorId == Guid.Empty
            ? ValidationError("شناسه پزشک مشخص نشده است.", "DoctorId is required.")
            : clinicId == Guid.Empty ? ValidationError("شناسه کلینیک مشخص نشده است.", "ClinicId is required.") : null;

    private IntegrationError MapProviderException(Exception exception) => exception is ConnectionError connectionError
            ? _errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = connectionError.StatusCode.HasValue ? (int)connectionError.StatusCode.Value : null,
                ProviderErrorCode = connectionError.StatusCode.HasValue ? ((int)connectionError.StatusCode.Value).ToString() : null,
                ProviderErrorMessage = connectionError.Content
            })
            : exception is UserLoginException loginException
            ? _errorMapper.MapError(new ProviderErrorContext
            {
                HttpStatusCode = loginException.Status,
                ProviderErrorCode = loginException.Status?.ToString(),
                ProviderErrorMessage = loginException.ReasonText ?? loginException.Message
            })
            : _errorMapper.MapError(new ProviderErrorContext
            {
                Exception = exception,
                ProviderErrorMessage = exception.Message
            });

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
