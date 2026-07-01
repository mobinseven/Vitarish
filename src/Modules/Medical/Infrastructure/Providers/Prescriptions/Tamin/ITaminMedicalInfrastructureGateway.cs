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

    Task<TaminProviderJsonResult> CheckPrescriptionWarningAsync(TaminWarningOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> CheckDentalRulesAsync(TaminWarningOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> GetReferralCountsAsync(TaminReferralCountOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> RecordReferralFeedbackAsync(TaminReferralFeedbackOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> GetNurseTodoListAsync(TaminNurseTodoListOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> RecordNurseActionAsync(TaminNurseActionOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> CreateHospitalizationAsync(TaminHospitalizationCreateOperationRequest request, CancellationToken cancellationToken);
    Task<TaminProviderJsonResult> GetHospitalizationSecretaryListAsync(TaminHospitalizationSecretaryListOperationRequest request, CancellationToken cancellationToken);
}
