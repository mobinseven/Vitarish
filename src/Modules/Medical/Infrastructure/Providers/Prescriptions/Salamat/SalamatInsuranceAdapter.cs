using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Domain.Enums;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Salamat;

/// <summary>
/// Salamat (سامانه سلامت / Ministry of Health e-Prescription portal) insurance provider adapter.
///
/// Salamat is an OAuth2-based system that supports:
/// - Entitlement check for patients covered by government health programs
/// - Full e-Prescription submission and tracking
/// - Edit and cancel of submitted prescriptions
/// - Drug and service reference data sync
///
/// NOTE: This adapter is currently a stub. The actual Salamat API endpoints will be wired up
/// once API access credentials are obtained from the Ministry of Health.
/// All operations return NotSupported until the provider is enabled via SalamatOptions.IsEnabled.
/// </summary>
public sealed class SalamatInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "SALAMAT";

    private readonly HttpClient _httpClient;
    private readonly SalamatOptions _options;
    private readonly ILogger<SalamatInsuranceAdapter> _logger;

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

    public SalamatInsuranceAdapter(
        HttpClient httpClient,
        IOptions<SalamatOptions> options,
        ILogger<SalamatInsuranceAdapter> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        string baseUrl = _options.UseSandbox && !string.IsNullOrEmpty(_options.SandboxBaseUrl)
            ? _options.SandboxBaseUrl
            : _options.BaseUrl;

        if (!string.IsNullOrEmpty(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }
    }

    public string ProviderKey => ProviderKeyConst;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => SupportedCapabilities;

    public Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; AuthenticateAsync returning NotSupported");
        return Task.FromResult(AdapterResult.NotSupported());
    }

    public Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; CheckEntitlementAsync returning NotSupported");
        return Task.FromResult(AdapterResult<EntitlementResponse>.NotSupported());
    }

    public Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; SubmitPrescriptionAsync returning NotSupported");
        return Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());
    }

    public Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; FetchPrescriptionAsync returning NotSupported");
        return Task.FromResult(AdapterResult<FetchResponse>.NotSupported());
    }

    public Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; EditPrescriptionAsync returning NotSupported");
        return Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());
    }

    public Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; CancelPrescriptionAsync returning NotSupported");
        return Task.FromResult(AdapterResult.NotSupported());
    }

    public Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Salamat adapter is a stub; FetchReferenceDataAsync returning NotSupported");
        return Task.FromResult(AdapterResult<ReferenceDataResponse>.NotSupported());
    }
}
