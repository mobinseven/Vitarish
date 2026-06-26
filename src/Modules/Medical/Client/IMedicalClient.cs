using Vitarish.Medical.Contracts.Imports;
using Vitarish.Medical.Contracts.Requests;
using Vitarish.Medical.Contracts.Responses;
using Vitarish.Medical.Contracts.Results;

namespace Vitarish.Medical.Client;

public interface IMedicalClient
{
    Task<MedicalResult<IReadOnlyList<CodeSystemResponse>>> ListCodeSystemsAsync(ListCodeSystemsRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<CodeSystemResponse?>> GetCodeSystemAsync(GetCodeSystemRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<CodeSystemReleaseResponse>>> ListCodeSystemReleasesAsync(ListCodeSystemReleasesRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<MedicalCodeSearchResultResponse>>> SearchMedicalCodesAsync(SearchMedicalCodesRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<MedicalCodeSearchResultResponse>>> SearchPrescriptionReferenceCodesAsync(SearchPrescriptionReferenceCodesRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<DrugSearchResultResponse>>> SearchDrugsAsync(SearchDrugsRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<MedicalServiceResponse>>> SearchMedicalServicesAsync(SearchMedicalServicesRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalImportResultResponse>> StartCodeSystemReleaseImportAsync(ImportCodeSystemReleaseRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<ImportBatchResponse?>> GetImportBatchAsync(GetImportBatchRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<PrescriptionDto?>> GetPrescriptionAsync(GetPrescriptionRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<PrescriptionSummaryDto>>> ListPatientPrescriptionsAsync(ListPatientPrescriptionsRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> CreatePrescriptionDraftAsync(CreatePrescriptionDraftRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> UpdatePrescriptionDraftAsync(UpdatePrescriptionDraftRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> AddPrescriptionItemAsync(AddPrescriptionItemRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> UpdatePrescriptionItemAsync(UpdatePrescriptionItemRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> RemovePrescriptionItemAsync(RemovePrescriptionItemRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> SelectPrescriptionProviderAsync(SelectPrescriptionProviderRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<EligibilityResultDto>> CheckPrescriptionEligibilityAsync(CheckPrescriptionEligibilityRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> SubmitPrescriptionAsync(SubmitPrescriptionRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> CancelPrescriptionAsync(CancelPrescriptionRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> StartPaperFallbackAsync(PaperFallbackRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<PrescriptionTemplateSummaryDto>>> ListPrescriptionTemplatesAsync(ListPrescriptionTemplatesRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<PrescriptionTemplateDto?>> GetPrescriptionTemplateAsync(GetPrescriptionTemplateRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> SavePrescriptionTemplateAsync(SavePrescriptionTemplateRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> ApplyPrescriptionTemplateAsync(ApplyPrescriptionTemplateRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<CredentialHealthResult>> ListProviderCredentialsAsync(OperationalDashboardRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> ConfigureProviderCredentialAsync(ConfigureProviderCredentialRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> SyncProviderReferenceDataAsync(SyncProviderReferenceDataRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<IReadOnlyList<OperationalAlertDto>>> GetPrescriptionOperationalDashboardAsync(OperationalDashboardRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<ReconciliationQueueResult>> GetReconciliationQueueAsync(ReconciliationQueueRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<OutboxStatusResult>> GetOutboxStatusAsync(OutboxStatusRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> RefreshPrescriptionProviderStatusAsync(RefreshProviderStatusRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> ResolveDuplicateRiskAsync(ResolveDuplicateRiskRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> ReconcilePaperFallbackAsync(ReconcilePaperFallbackRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> ManualReconcileAsync(ManualReconcileRequest request, CancellationToken cancellationToken);
    Task<MedicalResult<MedicalCommandResult>> EscalatePrescriptionAsync(EscalatePrescriptionRequest request, CancellationToken cancellationToken);
    Task<TResponse> Send<TResponse>(object request, CancellationToken cancellationToken = default);
}
