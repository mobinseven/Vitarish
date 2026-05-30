using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Infrastructure.Providers.Ditas;

/// <summary>
/// DITAS (Armed Forces Insurance Organization / بیمه نیروهای مسلح) insurance provider adapter.
/// Implements the canonical IInsuranceProviderAdapter contract.
///
/// NOTE: Entitlement check and prescription submission are the primary
/// operations; edit and cancel are available where the DITAS gateway supports them.
/// Reference data sync is not available via DITAS; codes are maintained manually.
/// </summary>
public sealed class DitasInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "DITAS";

    private readonly HttpClient _httpClient;
    private readonly DitasOptions _options;
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
        HttpClient httpClient,
        IOptions<DitasOptions> options,
        ILogger<DitasInsuranceAdapter> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        var baseUrl = _options.UseSandbox && !string.IsNullOrEmpty(_options.SandboxBaseUrl)
            ? _options.SandboxBaseUrl
            : _options.BaseUrl;

        if (!string.IsNullOrEmpty(baseUrl))
            _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public string ProviderKey => ProviderKeyConst;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => SupportedCapabilities;

    public async Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Authentication,
                UserMessage = "کلید دسترسی به سامانه نیروهای مسلح پیکربندی نشده است.",
                TechnicalDetail = "DitasOptions.ApiKey is empty",
                IsRetryable = false
            });
        }

        // DITAS uses an API-key-per-clinic model; no per-doctor session required.
        // Authentication is verified by sending a ping to the gateway health endpoint.
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, "api/health");
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("DITAS gateway health check failed for clinic {ClinicId}: {Status}",
                    request.ClinicId, response.StatusCode);
                return AdapterResult.Failure(new()
                {
                    Category = Modules.Base.Platform.Errors.ErrorCategory.Authentication,
                    UserMessage = "احراز هویت با سامانه نیروهای مسلح ناموفق بود.",
                    TechnicalDetail = $"HTTP {(int)response.StatusCode}",
                    IsRetryable = false
                });
            }

            _logger.LogInformation("DITAS authentication successful for clinic {ClinicId}", request.ClinicId);
            return AdapterResult.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "DITAS gateway unreachable for clinic {ClinicId}", request.ClinicId);
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
                TechnicalDetail = ex.Message,
                IsRetryable = true
            });
        }
    }

    public async Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"api/entitlement/{Uri.EscapeDataString(request.PatientNationalId)}");
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("DITAS entitlement check failed for patient {NationalId}: {Status} {Body}",
                    request.PatientNationalId, response.StatusCode, errorBody);

                return AdapterResult<EntitlementResponse>.Failure(new()
                {
                    Category = MapHttpStatusToCategory(response.StatusCode),
                    UserMessage = "بررسی حق بیمه بیمار در سامانه نیروهای مسلح ناموفق بود.",
                    TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                    ProviderErrorCode = ((int)response.StatusCode).ToString(),
                    IsRetryable = IsRetryableStatus(response.StatusCode)
                });
            }

            var result = await response.Content.ReadFromJsonAsync<DitasEntitlementResponse>(cancellationToken);

            return AdapterResult<EntitlementResponse>.Success(new EntitlementResponse
            {
                Status = MapDitasEligibility(result?.Status),
                TrackingCode = result?.TrackingCode,
                CoverageIdentifier = result?.MembershipNumber,
                RawStatusCode = result?.Status,
                RawStatusMessage = result?.StatusDescription,
                ExpiresAt = result?.ValidUntil
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "DITAS entitlement check network error for patient {NationalId}",
                request.PatientNationalId);
            return AdapterResult<EntitlementResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
                TechnicalDetail = ex.Message,
                IsRetryable = true
            });
        }
    }

    public async Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var submitPayload = new DitasPrescriptionSubmitRequest
            {
                PrescriptionId = request.PrescriptionId.ToString("N"),
                IdempotencyKey = request.IdempotencyKey,
                CorrelationId = request.CorrelationId
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/prescription/submit");
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);
            httpRequest.Headers.Add("X-Idempotency-Key", request.IdempotencyKey);
            httpRequest.Content = System.Net.Http.Json.JsonContent.Create(submitPayload);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("DITAS prescription submission failed for {PrescriptionId}: {Status} {Body}",
                    request.PrescriptionId, response.StatusCode, errorBody);

                return AdapterResult<SubmissionResponse>.Failure(new()
                {
                    Category = MapHttpStatusToCategory(response.StatusCode),
                    UserMessage = "ثبت نسخه در سامانه نیروهای مسلح ناموفق بود.",
                    TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                    ProviderErrorCode = ((int)response.StatusCode).ToString(),
                    IsRetryable = IsRetryableStatus(response.StatusCode)
                });
            }

            var result = await response.Content.ReadFromJsonAsync<DitasSubmissionResponse>(cancellationToken);
            _logger.LogInformation("DITAS prescription submitted successfully: {PrescriptionId} → {RemoteId}",
                request.PrescriptionId, result?.PrescriptionNumber);

            return AdapterResult<SubmissionResponse>.Success(new SubmissionResponse
            {
                RemotePrescriptionId = result?.PrescriptionNumber,
                RemoteTrackingCode = result?.TrackingCode,
                RemoteStatusCode = result?.Status,
                RemoteStatusText = result?.StatusDescription,
                CanonicalStatus = PrescriptionStatus.AcceptedByProvider
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "DITAS prescription submission network error for {PrescriptionId}",
                request.PrescriptionId);
            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
                TechnicalDetail = ex.Message,
                IsRetryable = true
            });
        }
    }

    public async Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.RemotePrescriptionId))
        {
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "شناسه نسخه در سامانه نیروهای مسلح مشخص نشده است.",
                TechnicalDetail = "RemotePrescriptionId is required for DITAS fetch",
                IsRetryable = false
            });
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                $"api/prescription/{Uri.EscapeDataString(request.RemotePrescriptionId)}");
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("DITAS fetch failed for {RemoteId}: {Status} {Body}",
                    request.RemotePrescriptionId, response.StatusCode, errorBody);

                return AdapterResult<FetchResponse>.Failure(new()
                {
                    Category = MapHttpStatusToCategory(response.StatusCode),
                    UserMessage = "دریافت وضعیت نسخه از سامانه نیروهای مسلح ناموفق بود.",
                    TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                    ProviderErrorCode = ((int)response.StatusCode).ToString(),
                    IsRetryable = IsRetryableStatus(response.StatusCode)
                });
            }

            var result = await response.Content.ReadFromJsonAsync<DitasFetchResponse>(cancellationToken);

            return AdapterResult<FetchResponse>.Success(new FetchResponse
            {
                RemoteStatusCode = result?.Status,
                RemoteStatusText = result?.StatusDescription,
                CanonicalStatus = MapDitasPrescriptionStatus(result?.Status),
                RemoteTrackingCode = result?.TrackingCode
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "DITAS fetch network error for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
                TechnicalDetail = ex.Message,
                IsRetryable = true
            });
        }
    }

    /// <summary>
    /// DITAS does not support prescription editing via API; edits require cancellation and re-submission.
    /// </summary>
    public Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());

    public async Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Delete,
                $"api/prescription/{Uri.EscapeDataString(request.RemotePrescriptionId)}");
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);
            httpRequest.Headers.Add("X-Correlation-Id", request.CorrelationId);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("DITAS cancel failed for {RemoteId}: {Status} {Body}",
                    request.RemotePrescriptionId, response.StatusCode, errorBody);

                return AdapterResult.Failure(new()
                {
                    Category = MapHttpStatusToCategory(response.StatusCode),
                    UserMessage = "لغو نسخه در سامانه نیروهای مسلح ناموفق بود.",
                    TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                    ProviderErrorCode = ((int)response.StatusCode).ToString(),
                    IsRetryable = IsRetryableStatus(response.StatusCode)
                });
            }

            _logger.LogInformation("DITAS prescription cancelled: {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult.Success();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "DITAS cancel network error for {RemoteId}", request.RemotePrescriptionId);
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Outage,
                UserMessage = "ارتباط با سامانه نیروهای مسلح برقرار نشد.",
                TechnicalDetail = ex.Message,
                IsRetryable = true
            });
        }
    }

    /// <summary>
    /// DITAS does not expose a reference data sync API; codes are maintained manually.
    /// </summary>
    public Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<ReferenceDataResponse>.NotSupported());

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EligibilityStatus MapDitasEligibility(string? status) => status switch
    {
        "ELIGIBLE" or "COVERED" => EligibilityStatus.Eligible,
        "NOT_ELIGIBLE" or "NOT_COVERED" => EligibilityStatus.NotEligible,
        "PENDING" => EligibilityStatus.RequiresManualVerification,
        _ => EligibilityStatus.Unknown
    };

    private static PrescriptionStatus MapDitasPrescriptionStatus(string? status) => status switch
    {
        "ACCEPTED" or "REGISTERED" => PrescriptionStatus.AcceptedByProvider,
        "CANCELLED" => PrescriptionStatus.Cancelled,
        "SUBMITTED" or "PENDING" => PrescriptionStatus.Submitted,
        _ => PrescriptionStatus.SyncPending
    };

    private static Modules.Base.Platform.Errors.ErrorCategory MapHttpStatusToCategory(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode switch
        {
            401 or 403 => Modules.Base.Platform.Errors.ErrorCategory.Authentication,
            400 => Modules.Base.Platform.Errors.ErrorCategory.Validation,
            409 => Modules.Base.Platform.Errors.ErrorCategory.DuplicateRisk,
            429 => Modules.Base.Platform.Errors.ErrorCategory.RateLimit,
            408 or 504 => Modules.Base.Platform.Errors.ErrorCategory.Timeout,
            502 or 503 => Modules.Base.Platform.Errors.ErrorCategory.Outage,
            _ => Modules.Base.Platform.Errors.ErrorCategory.Unknown
        };

    private static bool IsRetryableStatus(System.Net.HttpStatusCode statusCode) =>
        (int)statusCode is 408 or 429 or 502 or 503 or 504;
}

// ── DITAS-specific request/response DTOs ────────────────────────────────────

internal sealed record DitasEntitlementResponse
{
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
    public string? MembershipNumber { get; init; }
    public string? TrackingCode { get; init; }
    public DateTimeOffset? ValidUntil { get; init; }
}

internal sealed record DitasPrescriptionSubmitRequest
{
    public string? PrescriptionId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? CorrelationId { get; init; }
}

internal sealed record DitasSubmissionResponse
{
    public string? PrescriptionNumber { get; init; }
    public string? TrackingCode { get; init; }
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
}

internal sealed record DitasFetchResponse
{
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
    public string? TrackingCode { get; init; }
}
