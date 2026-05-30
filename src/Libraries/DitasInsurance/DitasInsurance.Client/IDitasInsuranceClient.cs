namespace Vitarish.DitasInsurance.Client;

public interface IDitasInsuranceClient
{
    Task CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<DitasEntitlementResponse?> CheckEntitlementAsync(string nationalId, CancellationToken cancellationToken = default);
    Task<DitasSubmissionResponse?> SubmitPrescriptionAsync(DitasPrescriptionSubmitRequest request, string idempotencyKey, CancellationToken cancellationToken = default);
    Task<DitasFetchResponse?> FetchPrescriptionAsync(string remotePrescriptionId, CancellationToken cancellationToken = default);
    Task CancelPrescriptionAsync(string remotePrescriptionId, string correlationId, CancellationToken cancellationToken = default);
}
