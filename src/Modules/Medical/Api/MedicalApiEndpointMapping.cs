using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Medical.Contracts.Errors;
using Vitarish.Medical.Contracts.Requests;
using Vitarish.Medical.Contracts.Routes;
using Vitarish.Modules.Base.Endpoints;

namespace Vitarish.Medical.Api;

public static class MedicalApiEndpointMapping
{
    public static IEndpointRouteBuilder MapMedicalApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(MedicalApiRoutes.Base);
        _ = group.MapPost(MedicalApiRoutes.CodeSystems + "/list", NotImplementedAsync<ListCodeSystemsRequest>);
        _ = group.MapPost(MedicalApiRoutes.CodeSystemById, NotImplementedAsync<GetCodeSystemRequest>);
        _ = group.MapPost(MedicalApiRoutes.CodeSystemReleases, NotImplementedAsync<ListCodeSystemReleasesRequest>);
        _ = group.MapPost(MedicalApiRoutes.CodeSystemReleaseImport, NotImplementedAsync<ImportCodeSystemReleaseRequest>);
        _ = group.MapPost(MedicalApiRoutes.MedicalCodeSearch, NotImplementedAsync<SearchMedicalCodesRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionReferenceCodeSearch, NotImplementedAsync<SearchPrescriptionReferenceCodesRequest>);
        _ = group.MapPost(MedicalApiRoutes.DrugSearch, NotImplementedAsync<SearchDrugsRequest>);
        _ = group.MapPost(MedicalApiRoutes.MedicalServiceSearch, NotImplementedAsync<SearchMedicalServicesRequest>);
        _ = group.MapPost(MedicalApiRoutes.ImportById, NotImplementedAsync<GetImportBatchRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionById, NotImplementedAsync<GetPrescriptionRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionHistory, NotImplementedAsync<ListPatientPrescriptionsRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionDrafts, NotImplementedAsync<CreatePrescriptionDraftRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionDrafts + "/update", NotImplementedAsync<UpdatePrescriptionDraftRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionItems, NotImplementedAsync<AddPrescriptionItemRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionItemById, NotImplementedAsync<UpdatePrescriptionItemRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionItemById + "/remove", NotImplementedAsync<RemovePrescriptionItemRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionProviderSelection, NotImplementedAsync<SelectPrescriptionProviderRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionEligibility, NotImplementedAsync<CheckPrescriptionEligibilityRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionSubmission, NotImplementedAsync<SubmitPrescriptionRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionCancellation, NotImplementedAsync<CancelPrescriptionRequest>);
        _ = group.MapPost(MedicalApiRoutes.PaperFallback, NotImplementedAsync<PaperFallbackRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionTemplates + "/list", NotImplementedAsync<ListPrescriptionTemplatesRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionTemplateById, NotImplementedAsync<GetPrescriptionTemplateRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionTemplates, NotImplementedAsync<SavePrescriptionTemplateRequest>);
        _ = group.MapPost(MedicalApiRoutes.PrescriptionTemplateApply, NotImplementedAsync<ApplyPrescriptionTemplateRequest>);
        _ = group.MapPost(MedicalApiRoutes.Credentials + "/list", NotImplementedAsync<OperationalDashboardRequest>);
        _ = group.MapPost(MedicalApiRoutes.Credentials, NotImplementedAsync<ConfigureProviderCredentialRequest>);
        _ = group.MapPost(MedicalApiRoutes.ProviderReferenceDataSync, NotImplementedAsync<SyncProviderReferenceDataRequest>);
        _ = group.MapPost(MedicalApiRoutes.ObservabilityDashboard, NotImplementedAsync<OperationalDashboardRequest>);
        _ = group.MapPost(MedicalApiRoutes.Reconciliation + "/queue", NotImplementedAsync<ReconciliationQueueRequest>);
        _ = group.MapPost(MedicalApiRoutes.OutboxStatus, NotImplementedAsync<OutboxStatusRequest>);
        _ = group.MapPost(MedicalApiRoutes.Reconciliation + "/refresh-provider-status", NotImplementedAsync<RefreshProviderStatusRequest>);
        _ = group.MapPost(MedicalApiRoutes.Reconciliation + "/resolve-duplicate-risk", NotImplementedAsync<ResolveDuplicateRiskRequest>);
        _ = group.MapPost(MedicalApiRoutes.Reconciliation + "/paper-fallback", NotImplementedAsync<ReconcilePaperFallbackRequest>);
        _ = group.MapPost(MedicalApiRoutes.Reconciliation + "/manual", NotImplementedAsync<ManualReconcileRequest>);
        _ = group.MapPost(MedicalApiRoutes.Escalation, NotImplementedAsync<EscalatePrescriptionRequest>);
        _ = group.MapPost("/compat", NotImplementedAsync<object>);
        return endpoints;
    }

    private static Task<IResult> NotImplementedAsync<TRequest>(TRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<IResult>(Results.StatusCode(StatusCodes.Status501NotImplemented));

    public static IResult ToHttpFailure(MedicalError error) => error.Code switch
    {
        MedicalErrorCode.ValidationFailure => Results.BadRequest(error),
        MedicalErrorCode.Unauthorized => Results.Unauthorized(),
        MedicalErrorCode.Forbidden => Results.Forbid(),
        MedicalErrorCode.NotFound => Results.NotFound(error),
        MedicalErrorCode.Conflict => Results.Conflict(error),
        MedicalErrorCode.ProviderUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
        MedicalErrorCode.Timeout => Results.StatusCode(StatusCodes.Status504GatewayTimeout),
        MedicalErrorCode.ProviderRejected => Results.UnprocessableEntity(error),
        MedicalErrorCode.CredentialInvalid or MedicalErrorCode.CredentialExpired or MedicalErrorCode.CredentialRevoked => Results.BadRequest(error),
        _ => Results.Problem(error.Message)
    };
}
