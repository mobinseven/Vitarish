namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal interface ITaminMedicalInfrastructureGateway
{
    Task<TaminReferencePageResult> SearchDrugsAsync(TaminDrugSearchOperationRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<TaminReferencePageResult> ListAllDrugPagesAsync(TaminDrugListAllOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> SearchServicesAsync(TaminServiceSearchOperationRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<TaminReferencePageResult> ListAllServicePagesAsync(TaminServiceListAllOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetAllServicesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetPrescriptionTypesAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetParaclinicTariffsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDrugAmountsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);
    Task<TaminReferencePageResult> GetDrugInstructionsAsync(TaminRawReferenceQueryOperationRequest request, CancellationToken cancellationToken);

}
