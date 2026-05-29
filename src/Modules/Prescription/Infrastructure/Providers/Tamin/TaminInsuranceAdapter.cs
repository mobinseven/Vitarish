using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Domain.Enums;
using Vitarish.Prescription.Infrastructure.Data;

namespace Vitarish.Prescription.Infrastructure.Providers.Tamin;

/// <summary>
/// Tamin (Social Security Organization) insurance provider adapter.
/// Implements authentication, entitlement check, prescription submission,
/// fetch, edit, cancel, and reference data sync operations.
/// </summary>
public sealed class TaminInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "TAMIN";

    private readonly HttpClient _httpClient;
    private readonly TaminOptions _options;
    private readonly PrescriptionDbContext _dbContext;
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
        HttpClient httpClient,
        IOptions<TaminOptions> options,
        PrescriptionDbContext dbContext,
        ILogger<TaminInsuranceAdapter> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _dbContext = dbContext;
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
        var credential = await _dbContext.DoctorProviderCredentials
            .Where(c => c.DoctorId == request.DoctorId && c.InsuranceProviderId == GetProviderId())
            .FirstOrDefaultAsync(cancellationToken);

        if (credential is null)
        {
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Authentication,
                UserMessage = "اطلاعات ورود پزشک برای تامین اجتماعی یافت نشد.",
                TechnicalDetail = $"No credential found for doctor {request.DoctorId} with provider TAMIN",
                IsRetryable = false
            });
        }

        var authRequest = new TaminAuthRequest
        {
            Username = credential.ProviderUsername,
            Password = credential.CredentialSecretRef
        };

        var response = await _httpClient.PostAsJsonAsync("api/auth/login", authRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tamin authentication failed for doctor {DoctorId} with status {StatusCode}",
                request.DoctorId, response.StatusCode);
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Authentication,
                UserMessage = "احراز هویت با سامانه تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}",
                IsRetryable = response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
            });
        }

        var authResponse = await response.Content.ReadFromJsonAsync<TaminAuthResponse>(cancellationToken);
        if (authResponse?.Token is not null)
        {
            credential.TokenSecretRef = authResponse.Token;
            credential.SessionId = authResponse.SessionId;
            credential.TokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(authResponse.ExpiresInMinutes ?? 30);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Tamin authentication successful for doctor {DoctorId}", request.DoctorId);
        return AdapterResult.Success();
    }

    public async Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        var session = await EnsureAuthenticatedAsync(request.DoctorId, request.ClinicId, cancellationToken);
        if (session is null)
            return AdapterResult<EntitlementResponse>.Failure(AuthenticationRequiredError());

        var entitlementRequest = new TaminEntitlementRequest
        {
            NationalCode = request.PatientNationalId
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/prescription/entitlement");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
        httpRequest.Content = JsonContent.Create(entitlementRequest);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Tamin entitlement check failed for patient {NationalId}: {Status} {Body}",
                request.PatientNationalId, response.StatusCode, errorBody);

            return AdapterResult<EntitlementResponse>.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "بررسی حق بیمه بیمار در تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                ProviderErrorCode = ((int)response.StatusCode).ToString(),
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        var result = await response.Content.ReadFromJsonAsync<TaminEntitlementResponse>(cancellationToken);

        return AdapterResult<EntitlementResponse>.Success(new EntitlementResponse
        {
            Status = MapTaminEligibility(result?.StatusCode),
            TrackingCode = result?.TrackingCode,
            CoverageIdentifier = result?.InsuranceNumber,
            RawStatusCode = result?.StatusCode,
            RawStatusMessage = result?.StatusMessage,
            ExpiresAt = result?.ExpiresAt
        });
    }

    public async Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "نسخه مورد نظر یافت نشد.",
                TechnicalDetail = $"Prescription {request.PrescriptionId} not found",
                IsRetryable = false
            });

        var session = await EnsureAuthenticatedAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (session is null)
            return AdapterResult<SubmissionResponse>.Failure(AuthenticationRequiredError());

        var taminRequest = MapToTaminSubmission(prescription);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/prescription/submit");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
        httpRequest.Headers.Add("X-Idempotency-Key", request.IdempotencyKey);
        httpRequest.Content = JsonContent.Create(taminRequest);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Tamin prescription submission failed for {PrescriptionId}: {Status} {Body}",
                request.PrescriptionId, response.StatusCode, errorBody);

            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "ثبت نسخه در تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                ProviderErrorCode = ((int)response.StatusCode).ToString(),
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        var result = await response.Content.ReadFromJsonAsync<TaminSubmissionResponse>(cancellationToken);

        return AdapterResult<SubmissionResponse>.Success(new SubmissionResponse
        {
            RemotePrescriptionId = result?.PrescriptionId,
            RemoteTrackingCode = result?.TrackingCode,
            RemoteStatusCode = result?.StatusCode,
            RemoteStatusText = result?.StatusMessage,
            CanonicalStatus = MapTaminSubmissionStatus(result?.StatusCode)
        });
    }

    public async Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.RemotePrescriptionId))
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "شناسه نسخه ریموت مشخص نشده است.",
                TechnicalDetail = "RemotePrescriptionId is required for fetch",
                IsRetryable = false
            });

        // We need a doctor session - find from the submission record
        var submission = await _dbContext.PrescriptionSubmissions
            .Where(s => s.RemotePrescriptionId == request.RemotePrescriptionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "سابقه ثبت نسخه یافت نشد.",
                TechnicalDetail = $"No submission record for remote ID {request.RemotePrescriptionId}",
                IsRetryable = false
            });

        var prescription = await _dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == submission.PrescriptionRecordId, cancellationToken);

        if (prescription is null)
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "نسخه مرتبط یافت نشد.",
                IsRetryable = false
            });

        var session = await EnsureAuthenticatedAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (session is null)
            return AdapterResult<FetchResponse>.Failure(AuthenticationRequiredError());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get,
            $"api/prescription/{request.RemotePrescriptionId}");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return AdapterResult<FetchResponse>.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "دریافت وضعیت نسخه از تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        var result = await response.Content.ReadFromJsonAsync<TaminFetchResponse>(cancellationToken);

        return AdapterResult<FetchResponse>.Success(new FetchResponse
        {
            RemoteStatusCode = result?.StatusCode,
            RemoteStatusText = result?.StatusMessage,
            CanonicalStatus = MapTaminSubmissionStatus(result?.StatusCode),
            RemoteTrackingCode = result?.TrackingCode
        });
    }

    public async Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
    {
        var prescription = await _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PrescriptionId, cancellationToken);

        if (prescription is null)
            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "نسخه مورد نظر یافت نشد.",
                IsRetryable = false
            });

        var submission = await _dbContext.PrescriptionSubmissions
            .Where(s => s.PrescriptionRecordId == prescription.Id)
            .OrderByDescending(s => s.SubmittedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission?.RemotePrescriptionId is null)
            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.BusinessRule,
                UserMessage = "نسخه هنوز در سامانه ثبت نشده و قابل ویرایش نیست.",
                IsRetryable = false
            });

        var session = await EnsureAuthenticatedAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (session is null)
            return AdapterResult<SubmissionResponse>.Failure(AuthenticationRequiredError());

        var taminRequest = MapToTaminSubmission(prescription);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Put,
            $"api/prescription/{submission.RemotePrescriptionId}");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);
        httpRequest.Headers.Add("X-Idempotency-Key", request.IdempotencyKey);
        httpRequest.Content = JsonContent.Create(taminRequest);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return AdapterResult<SubmissionResponse>.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "ویرایش نسخه در تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        var result = await response.Content.ReadFromJsonAsync<TaminSubmissionResponse>(cancellationToken);

        return AdapterResult<SubmissionResponse>.Success(new SubmissionResponse
        {
            RemotePrescriptionId = result?.PrescriptionId ?? submission.RemotePrescriptionId,
            RemoteTrackingCode = result?.TrackingCode,
            RemoteStatusCode = result?.StatusCode,
            RemoteStatusText = result?.StatusMessage,
            CanonicalStatus = MapTaminSubmissionStatus(result?.StatusCode)
        });
    }

    public async Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.RemotePrescriptionId))
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "شناسه نسخه ریموت مشخص نشده است.",
                IsRetryable = false
            });

        // Find doctor from submission to get session
        var submission = await _dbContext.PrescriptionSubmissions
            .Where(s => s.RemotePrescriptionId == request.RemotePrescriptionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (submission is null)
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "سابقه ثبت نسخه یافت نشد.",
                IsRetryable = false
            });

        var prescription = await _dbContext.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == submission.PrescriptionRecordId, cancellationToken);

        if (prescription is null)
            return AdapterResult.Failure(new()
            {
                Category = Modules.Base.Platform.Errors.ErrorCategory.Validation,
                UserMessage = "نسخه مرتبط یافت نشد.",
                IsRetryable = false
            });

        var session = await EnsureAuthenticatedAsync(prescription.DoctorId, prescription.ClinicId, cancellationToken);
        if (session is null)
            return AdapterResult.Failure(AuthenticationRequiredError());

        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete,
            $"api/prescription/{request.RemotePrescriptionId}");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", session.Token);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return AdapterResult.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "لغو نسخه در تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        return AdapterResult.Success();
    }

    public async Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"api/reference/{request.CodeSystem}";
        if (!string.IsNullOrEmpty(request.SinceVersion))
            url += $"?sinceVersion={Uri.EscapeDataString(request.SinceVersion)}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        if (!string.IsNullOrEmpty(_options.ApiKey))
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return AdapterResult<ReferenceDataResponse>.Failure(new()
            {
                Category = MapHttpStatusToCategory(response.StatusCode),
                UserMessage = "دریافت داده‌های مرجع از تامین اجتماعی ناموفق بود.",
                TechnicalDetail = $"HTTP {(int)response.StatusCode}: {errorBody}",
                IsRetryable = IsRetryableStatus(response.StatusCode)
            });
        }

        var result = await response.Content.ReadFromJsonAsync<TaminReferenceDataResponse>(cancellationToken);

        var items = result?.Items?.Select(i => new ReferenceDataItem
        {
            Code = i.Code,
            DisplayName = i.Name,
            Category = i.Category,
            IsActive = i.IsActive
        }).ToList() ?? [];

        return AdapterResult<ReferenceDataResponse>.Success(new ReferenceDataResponse
        {
            CodeSystem = request.CodeSystem,
            Version = result?.Version ?? "unknown",
            Items = items
        });
    }

    #region Private Helpers

    private async Task<TaminSession?> EnsureAuthenticatedAsync(Guid doctorId, Guid clinicId, CancellationToken cancellationToken)
    {
        var credential = await _dbContext.DoctorProviderCredentials
            .Where(c => c.DoctorId == doctorId && c.InsuranceProviderId == GetProviderId())
            .FirstOrDefaultAsync(cancellationToken);

        if (credential is null)
            return null;

        // Check if session is still valid
        if (!string.IsNullOrEmpty(credential.TokenSecretRef) &&
            credential.TokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2))
        {
            return new TaminSession { Token = credential.TokenSecretRef, SessionId = credential.SessionId };
        }

        // Re-authenticate
        var authResult = await AuthenticateAsync(new AuthenticateRequest
        {
            DoctorId = doctorId,
            ClinicId = clinicId
        }, cancellationToken);

        if (!authResult.IsSuccess)
            return null;

        // Reload credential after auth
        await _dbContext.Entry(credential).ReloadAsync(cancellationToken);
        return new TaminSession { Token = credential.TokenSecretRef!, SessionId = credential.SessionId };
    }

    private Guid GetProviderId()
    {
        // This would normally be cached or injected; simplified for now
        var provider = _dbContext.InsuranceProviders
            .FirstOrDefault(p => p.ProviderKey == ProviderKeyConst);
        return provider?.Id ?? Guid.Empty;
    }

    private static TaminPrescriptionSubmitRequest MapToTaminSubmission(Domain.Entities.PrescriptionRecord prescription)
    {
        return new TaminPrescriptionSubmitRequest
        {
            PatientId = prescription.PatientId.ToString(),
            Items = prescription.Items.Select(i => new TaminPrescriptionItem
            {
                Code = i.ReferenceCode ?? string.Empty,
                Name = i.DisplayName ?? string.Empty,
                Category = MapItemCategory(i.Category),
                Quantity = i.Quantity,
                DurationDays = i.DurationDays,
                DosageInfo = i.DosageJson
            }).ToList()
        };
    }

    private static string MapItemCategory(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Visit => "visit",
        PrescriptionItemCategory.Drug => "drug",
        PrescriptionItemCategory.Lab => "lab",
        PrescriptionItemCategory.Imaging => "imaging",
        PrescriptionItemCategory.Paraclinic => "paraclinic",
        PrescriptionItemCategory.MedicalService => "service",
        PrescriptionItemCategory.Physiotherapy => "physio",
        PrescriptionItemCategory.Dental => "dental",
        PrescriptionItemCategory.Referral => "referral",
        PrescriptionItemCategory.MedicalEquipment => "equipment",
        _ => "other"
    };

    private static EligibilityStatus MapTaminEligibility(string? statusCode) => statusCode switch
    {
        "ELIGIBLE" or "1" => EligibilityStatus.Eligible,
        "NOT_ELIGIBLE" or "0" => EligibilityStatus.NotEligible,
        "REQUIRES_CONFIRMATION" => EligibilityStatus.RequiresPatientConfirmation,
        _ => EligibilityStatus.Unknown
    };

    private static PrescriptionStatus MapTaminSubmissionStatus(string? statusCode) => statusCode switch
    {
        "ACCEPTED" or "1" => PrescriptionStatus.AcceptedByProvider,
        "REJECTED" or "0" => PrescriptionStatus.RejectedByProvider,
        "PENDING" or "2" => PrescriptionStatus.Submitted,
        _ => PrescriptionStatus.SyncPending
    };

    private static Modules.Base.Platform.Errors.ErrorCategory MapHttpStatusToCategory(System.Net.HttpStatusCode status) => status switch
    {
        System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden => Modules.Base.Platform.Errors.ErrorCategory.Authentication,
        System.Net.HttpStatusCode.BadRequest => Modules.Base.Platform.Errors.ErrorCategory.Validation,
        System.Net.HttpStatusCode.Conflict => Modules.Base.Platform.Errors.ErrorCategory.DuplicateRisk,
        System.Net.HttpStatusCode.TooManyRequests => Modules.Base.Platform.Errors.ErrorCategory.RateLimit,
        System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.GatewayTimeout => Modules.Base.Platform.Errors.ErrorCategory.Timeout,
        System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.BadGateway => Modules.Base.Platform.Errors.ErrorCategory.Outage,
        _ => Modules.Base.Platform.Errors.ErrorCategory.Unknown
    };

    private static bool IsRetryableStatus(System.Net.HttpStatusCode status) => status is
        System.Net.HttpStatusCode.RequestTimeout or
        System.Net.HttpStatusCode.TooManyRequests or
        System.Net.HttpStatusCode.ServiceUnavailable or
        System.Net.HttpStatusCode.GatewayTimeout or
        System.Net.HttpStatusCode.BadGateway;

    private static Modules.Base.Platform.Errors.IntegrationError AuthenticationRequiredError() => new()
    {
        Category = Modules.Base.Platform.Errors.ErrorCategory.Authentication,
        UserMessage = "نیاز به ورود مجدد به سامانه تامین اجتماعی است.",
        TechnicalDetail = "Session expired or credentials not found",
        IsRetryable = false
    };

    #endregion
}

#region Tamin API DTOs

internal sealed class TaminSession
{
    public required string Token { get; init; }
    public string? SessionId { get; init; }
}

internal sealed class TaminAuthRequest
{
    public required string Username { get; init; }
    public required string Password { get; init; }
}

internal sealed class TaminAuthResponse
{
    public string? Token { get; init; }
    public string? SessionId { get; init; }
    public int? ExpiresInMinutes { get; init; }
}

internal sealed class TaminEntitlementRequest
{
    public required string NationalCode { get; init; }
}

internal sealed class TaminEntitlementResponse
{
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public string? TrackingCode { get; init; }
    public string? InsuranceNumber { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class TaminPrescriptionSubmitRequest
{
    public required string PatientId { get; init; }
    public required List<TaminPrescriptionItem> Items { get; init; }
}

internal sealed class TaminPrescriptionItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public int Quantity { get; init; }
    public int? DurationDays { get; init; }
    public string? DosageInfo { get; init; }
}

internal sealed class TaminSubmissionResponse
{
    public string? PrescriptionId { get; init; }
    public string? TrackingCode { get; init; }
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
}

internal sealed class TaminFetchResponse
{
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public string? TrackingCode { get; init; }
}

internal sealed class TaminReferenceDataResponse
{
    public string? Version { get; init; }
    public List<TaminReferenceItem>? Items { get; init; }
}

internal sealed class TaminReferenceItem
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Category { get; init; }
    public bool IsActive { get; init; }
}

#endregion
