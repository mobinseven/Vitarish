using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Ditas;

/// <summary>
/// Stub adapter for the removed DITAS insurance integration.
/// </summary>
public sealed class DitasInsuranceAdapter : IInsuranceProviderAdapter
{
    public const string ProviderKeyConst = "DITAS";

    private static readonly IReadOnlySet<InsuranceProviderCapabilityType> SupportedCapabilities =
        new HashSet<InsuranceProviderCapabilityType>();

    public string ProviderKey => ProviderKeyConst;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities() => SupportedCapabilities;

    public Task<AdapterResult> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult.NotSupported());

    public Task<AdapterResult<EntitlementResponse>> CheckEntitlementAsync(EntitlementRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<EntitlementResponse>.NotSupported());

    public Task<AdapterResult<SubmissionResponse>> SubmitPrescriptionAsync(SubmissionRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());

    public Task<AdapterResult<FetchResponse>> FetchPrescriptionAsync(FetchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<FetchResponse>.NotSupported());

    public Task<AdapterResult<SubmissionResponse>> EditPrescriptionAsync(EditRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<SubmissionResponse>.NotSupported());

    public Task<AdapterResult> CancelPrescriptionAsync(CancelRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult.NotSupported());

    public Task<AdapterResult<ReferenceDataResponse>> FetchReferenceDataAsync(ReferenceDataRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(AdapterResult<ReferenceDataResponse>.NotSupported());
}
