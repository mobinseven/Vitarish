using Tamin.Integration;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal interface ITaminMedicalInfrastructureGateway
{
    Task<TaminReferencePageResult> SearchDrugsAsync(ITaminClient client, TaminDrugSearchOperationRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(ITaminClient client, TaminDrugListAllOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> SearchServicesAsync(ITaminClient client, TaminServiceSearchOperationRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(ITaminClient client, TaminServiceListAllOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetAllServicesAsync(ITaminClient client, TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetPrescriptionTypesAsync(ITaminClient client, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetParaclinicTariffsAsync(ITaminClient client, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDrugAmountsAsync(ITaminClient client, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDrugInstructionsAsync(ITaminClient client, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDrugUsagesAsync(ITaminClient client, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDosageFormsAsync(ITaminClient client, CancellationToken cancellationToken);
}
