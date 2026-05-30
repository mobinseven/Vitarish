namespace Vitarish.TaminInsurance.Client;

public interface ITaminInsuranceClient
{
    Task<TaminAuthResponse?> AuthenticateAsync(TaminAuthRequest request, CancellationToken cancellationToken = default);
    Task<TaminEntitlementResponse?> CheckEntitlementAsync(string bearerToken, TaminEntitlementRequest request, CancellationToken cancellationToken = default);
    Task<TaminSubmissionResponse?> SubmitPrescriptionAsync(string bearerToken, TaminPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TaminSubmissionResponse?> EditPrescriptionAsync(string bearerToken, string remotePrescriptionId, TaminPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<TaminFetchResponse?> FetchPrescriptionAsync(string bearerToken, string remotePrescriptionId, CancellationToken cancellationToken = default);
    Task CancelPrescriptionAsync(string bearerToken, string remotePrescriptionId, CancellationToken cancellationToken = default);
    Task<TaminReferenceDataResponse?> FetchReferenceDataAsync(string codeSystem, string? sinceVersion = null, CancellationToken cancellationToken = default);
}
