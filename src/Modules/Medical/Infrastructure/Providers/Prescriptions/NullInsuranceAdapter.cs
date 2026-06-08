using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions;

/// <summary>
/// Adapter for disabled or manual-only providers. Returns graceful "not available"
/// for all operations without throwing exceptions.
/// </summary>
public sealed class NullInsuranceAdapter(string providerKey) : IInsuranceProviderAdapter
{
    public string ProviderKey { get; } = providerKey;

    public IReadOnlySet<InsuranceProviderCapabilityType> GetCapabilities()
        => new HashSet<InsuranceProviderCapabilityType>();

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
