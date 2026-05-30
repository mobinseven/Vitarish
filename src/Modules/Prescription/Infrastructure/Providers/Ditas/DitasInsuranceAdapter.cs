using Microsoft.Extensions.Logging;
using Vitarish.DitasInsurance.Client;
using Vitarish.Modules.Base.Platform.Errors;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Providers.Ditas;

/// <summary>
/// DITAS module adapter. Keeps provider HTTP/API-key details in the DITAS client and
/// translates between canonical Prescription contracts and provider DTOs.
/// </summary>
public sealed class DitasInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "DITAS";

    private readonly IDitasInsuranceClient _client;
    private readonly DitasErrorMapper _errorMapper;
    private readonly ILogger<DitasInsuranceAdapter> _logger;

    private static readonly HashSet<InsuranceProviderCapabilityType> SupportedCapabilities =
    [
        InsuranceProviderCapabilityType.SupportsEntitlementCheck,
        InsuranceProviderCapabilityType.SupportsVisitPrescription,
        InsuranceProviderCapabilityType.SupportsDrugPrescription,
        InsuranceProviderCapabilityType.SupportsParaclinicPrescription,
        InsuranceProviderCapabilityType.SupportsFetchPrescription,
        InsuranceProviderCapabilityType.SupportsCancelPrescription,
        InsuranceProviderCapabilityType.SupportsProviderSession,
    ];

    public DitasInsuranceAdapter(
        IDitasInsuranceClient client,
        DitasErrorMapper errorMapper,
        ILogger<DitasInsuranceAdapter> logger)
    {
        _client = client;
        _errorMapper = errorMapper;
        _logger = logger;
    }


    public string ProviderKey => ProviderKeyConst;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => SupportedCapabilities;

    public async Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.CheckHealthAsync(cancellationToken);
            _logger.LogInformation("DITAS authentication successful for clinic {ClinicId}", request.ClinicId);
            return AdapterResult.Success();
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "DITAS authentication failed for clinic {ClinicId}", request.ClinicId);
            return AdapterResult.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientNationalId))
            return AdapterResult<EntitlementResponse>.Failure(ValidationError("کد ملی بیمار مشخص نشده است.", "PatientNationalId is required."));

        try
        {
            var result = await _client.CheckEntitlementAsync(request.PatientNationalId, cancellationToken);
            return AdapterResult<EntitlementResponse>.Success(DitasPrescriptionMapper.ToEntitlementResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "DITAS entitlement check failed for patient {NationalId}", request.PatientNationalId);
            return AdapterResult<EntitlementResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PrescriptionId == Guid.Empty)
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("شناسه نسخه مشخص نشده است.", "PrescriptionId is required."));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return AdapterResult<SubmissionResponse>.Failure(ValidationError("کلید یکتایی درخواست مشخص نشده است.", "IdempotencyKey is required."));

        try
        {
            var result = await _client.SubmitPrescriptionAsync(
                DitasPrescriptionMapper.ToDitasSubmission(request),
                request.IdempotencyKey,
                cancellationToken);

            _logger.LogInformation("DITAS prescription submitted successfully: {PrescriptionId} → {RemoteId}",
                request.PrescriptionId, result?.PrescriptionNumber);

            return AdapterResult<SubmissionResponse>.Success(DitasPrescriptionMapper.ToSubmissionResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "DITAS prescription submission failed for {PrescriptionId}", request.PrescriptionId);
            return AdapterResult<SubmissionResponse>.Failure(MapProviderException(ex));
        }
    }

    public async Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RemotePrescriptionId))
            return AdapterResult<FetchResponse>.Failure(ValidationError("شناسه نسخه در سامانه نیروهای مسلح مشخص نشده است.", "RemotePrescriptionId is required for DITAS fetch."));

        try
        {
            var result = await _client.FetchPrescriptionAsync(request.RemotePrescriptionId, cancellationToken);
            return AdapterResult<FetchResponse>.Success(DitasPrescriptionMapper.ToFetchResponse(result));
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "DITAS fetch failed for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult<FetchResponse>.Failure(MapProviderException(ex));
        }
    }

    /// <summary>
    /// DITAS does not support prescription editing via API; edits require cancellation and re-submission.
    /// </summary>
    public Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());

    public async Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RemotePrescriptionId))
            return AdapterResult.Failure(ValidationError("شناسه نسخه در سامانه نیروهای مسلح مشخص نشده است.", "RemotePrescriptionId is required."));
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
            return AdapterResult.Failure(ValidationError("شناسه رهگیری درخواست مشخص نشده است.", "CorrelationId is required."));

        try
        {
            await _client.CancelPrescriptionAsync(request.RemotePrescriptionId, request.CorrelationId, cancellationToken);
            _logger.LogInformation("DITAS prescription cancelled: {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult.Success();
        }
        catch (Exception ex) when (IsProviderException(ex))
        {
            _logger.LogWarning(ex, "DITAS cancel failed for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult.Failure(MapProviderException(ex));
        }
    }

    /// <summary>
    /// DITAS does not expose a reference data sync API; codes are maintained manually.
    /// </summary>
    public Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<ReferenceDataResponse>.NotSupported());

    private IntegrationError MapProviderException(Exception exception)
    {
        if (exception is DitasInsuranceClientException clientException)
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
        exception is DitasInsuranceClientException or HttpRequestException or TaskCanceledException;

    private static IntegrationError ValidationError(string userMessage, string technicalDetail) => new()
    {
        Category = ErrorCategory.Validation,
        UserMessage = userMessage,
        TechnicalDetail = technicalDetail,
        IsRetryable = false
    };
}
