using System.Net;
using Vitarish.Medical.Contracts.Errors;
using Vitarish.Medical.Contracts.Imports;
using Vitarish.Medical.Contracts.Requests;
using Vitarish.Medical.Contracts.Responses;
using Vitarish.Medical.Contracts.Results;
using Vitarish.Medical.Contracts.Routes;
using Vitarish.Modules.Base.Client;

namespace Vitarish.Medical.Client;

public sealed class MedicalClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), IMedicalClient
{
    public Task<MedicalResult<IReadOnlyList<CodeSystemResponse>>> ListCodeSystemsAsync(ListCodeSystemsRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<CodeSystemResponse>>(MedicalApiRoutes.CodeSystems + "/list", request, cancellationToken);
    public Task<MedicalResult<CodeSystemResponse?>> GetCodeSystemAsync(GetCodeSystemRequest request, CancellationToken cancellationToken) => Post<CodeSystemResponse?>(MedicalApiRoutes.CodeSystemById.Replace("{codeSystemId:guid}", request.CodeSystemId.ToString()), request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<CodeSystemReleaseResponse>>> ListCodeSystemReleasesAsync(ListCodeSystemReleasesRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<CodeSystemReleaseResponse>>(MedicalApiRoutes.CodeSystemReleases.Replace("{codeSystemId:guid}", request.CodeSystemId.ToString()), request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<MedicalCodeSearchResultResponse>>> SearchMedicalCodesAsync(SearchMedicalCodesRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<MedicalCodeSearchResultResponse>>(MedicalApiRoutes.MedicalCodeSearch, request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<MedicalCodeSearchResultResponse>>> SearchPrescriptionReferenceCodesAsync(SearchPrescriptionReferenceCodesRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<MedicalCodeSearchResultResponse>>(MedicalApiRoutes.PrescriptionReferenceCodeSearch, request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<DrugSearchResultResponse>>> SearchDrugsAsync(SearchDrugsRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<DrugSearchResultResponse>>(MedicalApiRoutes.DrugSearch, request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<MedicalServiceResponse>>> SearchMedicalServicesAsync(SearchMedicalServicesRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<MedicalServiceResponse>>(MedicalApiRoutes.MedicalServiceSearch, request, cancellationToken);
    public Task<MedicalResult<MedicalImportResultResponse>> StartCodeSystemReleaseImportAsync(ImportCodeSystemReleaseRequest request, CancellationToken cancellationToken) => Post<MedicalImportResultResponse>(MedicalApiRoutes.CodeSystemReleaseImport, request, cancellationToken);
    public Task<MedicalResult<ImportBatchResponse?>> GetImportBatchAsync(GetImportBatchRequest request, CancellationToken cancellationToken) => Post<ImportBatchResponse?>(MedicalApiRoutes.ImportById.Replace("{importBatchId:guid}", request.ImportBatchId.ToString()), request, cancellationToken);
    public Task<MedicalResult<PrescriptionDto?>> GetPrescriptionAsync(GetPrescriptionRequest request, CancellationToken cancellationToken) => Post<PrescriptionDto?>(MedicalApiRoutes.PrescriptionById.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<PrescriptionSummaryDto>>> ListPatientPrescriptionsAsync(ListPatientPrescriptionsRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<PrescriptionSummaryDto>>(MedicalApiRoutes.PrescriptionHistory, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> CreatePrescriptionDraftAsync(CreatePrescriptionDraftRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionDrafts, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> UpdatePrescriptionDraftAsync(UpdatePrescriptionDraftRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionDrafts + "/update", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> AddPrescriptionItemAsync(AddPrescriptionItemRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionItems.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> UpdatePrescriptionItemAsync(UpdatePrescriptionItemRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionItemById.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()).Replace("{itemId:guid}", request.ItemId.ToString()), request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> RemovePrescriptionItemAsync(RemovePrescriptionItemRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionItemById.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()).Replace("{itemId:guid}", request.ItemId.ToString()) + "/remove", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> SelectPrescriptionProviderAsync(SelectPrescriptionProviderRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionProviderSelection.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<EligibilityResultDto>> CheckPrescriptionEligibilityAsync(CheckPrescriptionEligibilityRequest request, CancellationToken cancellationToken) => Post<EligibilityResultDto>(MedicalApiRoutes.PrescriptionEligibility, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> SubmitPrescriptionAsync(SubmitPrescriptionRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionSubmission.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> CancelPrescriptionAsync(CancelPrescriptionRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionCancellation.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> StartPaperFallbackAsync(PaperFallbackRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PaperFallback.Replace("{prescriptionId:guid}", request.PrescriptionId.ToString()), request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<PrescriptionTemplateSummaryDto>>> ListPrescriptionTemplatesAsync(ListPrescriptionTemplatesRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<PrescriptionTemplateSummaryDto>>(MedicalApiRoutes.PrescriptionTemplates + "/list", request, cancellationToken);
    public Task<MedicalResult<PrescriptionTemplateDto?>> GetPrescriptionTemplateAsync(GetPrescriptionTemplateRequest request, CancellationToken cancellationToken) => Post<PrescriptionTemplateDto?>(MedicalApiRoutes.PrescriptionTemplateById.Replace("{templateId:guid}", request.TemplateId.ToString()), request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> SavePrescriptionTemplateAsync(SavePrescriptionTemplateRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionTemplates, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> ApplyPrescriptionTemplateAsync(ApplyPrescriptionTemplateRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.PrescriptionTemplateApply.Replace("{templateId:guid}", request.TemplateId.ToString()), request, cancellationToken);
    public Task<MedicalResult<CredentialHealthResult>> ListProviderCredentialsAsync(OperationalDashboardRequest request, CancellationToken cancellationToken) => Post<CredentialHealthResult>(MedicalApiRoutes.Credentials + "/list", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> ConfigureProviderCredentialAsync(ConfigureProviderCredentialRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Credentials, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> SyncProviderReferenceDataAsync(SyncProviderReferenceDataRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.ProviderReferenceDataSync, request, cancellationToken);
    public Task<MedicalResult<IReadOnlyList<OperationalAlertDto>>> GetPrescriptionOperationalDashboardAsync(OperationalDashboardRequest request, CancellationToken cancellationToken) => Post<IReadOnlyList<OperationalAlertDto>>(MedicalApiRoutes.ObservabilityDashboard, request, cancellationToken);
    public Task<MedicalResult<ReconciliationQueueResult>> GetReconciliationQueueAsync(ReconciliationQueueRequest request, CancellationToken cancellationToken) => Post<ReconciliationQueueResult>(MedicalApiRoutes.Reconciliation + "/queue", request, cancellationToken);
    public Task<MedicalResult<OutboxStatusResult>> GetOutboxStatusAsync(OutboxStatusRequest request, CancellationToken cancellationToken) => Post<OutboxStatusResult>(MedicalApiRoutes.OutboxStatus, request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> RefreshPrescriptionProviderStatusAsync(RefreshProviderStatusRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Reconciliation + "/refresh-provider-status", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> ResolveDuplicateRiskAsync(ResolveDuplicateRiskRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Reconciliation + "/resolve-duplicate-risk", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> ReconcilePaperFallbackAsync(ReconcilePaperFallbackRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Reconciliation + "/paper-fallback", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> ManualReconcileAsync(ManualReconcileRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Reconciliation + "/manual", request, cancellationToken);
    public Task<MedicalResult<MedicalCommandResult>> EscalatePrescriptionAsync(EscalatePrescriptionRequest request, CancellationToken cancellationToken) => Post<MedicalCommandResult>(MedicalApiRoutes.Escalation, request, cancellationToken);

    public async Task<TResponse> Send<TResponse>(object request, CancellationToken cancellationToken = default)
    {
        MedicalResult<TResponse> result = await Post<TResponse>("/compat", request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new HttpRequestException($"Compatibility request failed: {result.Failure?.Message} (Code: {result.Failure?.Code})");
        }
        return result.Value!;
    }

    private async Task<MedicalResult<T>> Post<T>(string route, object request, CancellationToken cancellationToken)
    {
        var result = await PostJsonAsync<object, T>(MedicalApiRoutes.Base + route, request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? MedicalResult<T>.Success(result.Value!) : MedicalResult<T>.Failed(MapFailure(result.Failure!));
    }

    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);
    private static MedicalError MapFailure(ModuleApiClientFailure failure) => new(MapCode(failure), failure.Message, failure.StatusCode);
    private static MedicalErrorCode MapCode(ModuleApiClientFailure failure) => failure.Code switch
    {
        "Canceled" => MedicalErrorCode.Cancellation,
        "Timeout" => MedicalErrorCode.Timeout,
        "Deserialization" => MedicalErrorCode.DeserializationFailure,
        nameof(HttpStatusCode.BadRequest) => MedicalErrorCode.ValidationFailure,
        nameof(HttpStatusCode.Unauthorized) => MedicalErrorCode.Unauthorized,
        nameof(HttpStatusCode.Forbidden) => MedicalErrorCode.Forbidden,
        nameof(HttpStatusCode.NotFound) => MedicalErrorCode.NotFound,
        nameof(HttpStatusCode.Conflict) => MedicalErrorCode.Conflict,
        nameof(HttpStatusCode.ServiceUnavailable) => MedicalErrorCode.ProviderUnavailable,
        nameof(HttpStatusCode.GatewayTimeout) => MedicalErrorCode.Timeout,
        _ => MedicalErrorCode.UnexpectedFailure
    };
}
