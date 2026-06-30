using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Medical.Contracts.Enums;
using Vitarish.Medical.Contracts.Errors;
using Vitarish.Medical.Contracts.Imports;
using Vitarish.Medical.Contracts.Requests;
using Vitarish.Medical.Contracts.Responses;
using Vitarish.Medical.Contracts.Results;
using Vitarish.Medical.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;
using A = Vitarish.Medical.Application.Commands;
using AP = Vitarish.Medical.Application.Commands.Prescriptions;
using AQ = Vitarish.Medical.Application.Queries.Prescriptions;
using D = Vitarish.Medical.Domain.Entities;
using DE = Vitarish.Medical.Domain.Enums;
using DEP = Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Api;

public static class MedicalApiEndpointMapping
{
    public static IEndpointRouteBuilder MapMedicalApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(MedicalApiRoutes.Base);
        _ = group.MapModulePost(MedicalApiRoutes.CodeSystems + "/list", ListCodeSystemsAsync, "MedicalListCodeSystems");
        _ = group.MapModulePost(MedicalApiRoutes.CodeSystemById, GetCodeSystemAsync, "MedicalGetCodeSystem");
        _ = group.MapModulePost(MedicalApiRoutes.CodeSystemByKey, GetCodeSystemByKeyAsync, "MedicalGetCodeSystemByKey");
        _ = group.MapModulePost(MedicalApiRoutes.CodeSystemReleases, ListCodeSystemReleasesAsync, "MedicalListCodeSystemReleases");
        _ = group.MapModulePost(MedicalApiRoutes.ActiveCodeSystemRelease, GetActiveCodeSystemReleaseAsync, "MedicalGetActiveCodeSystemRelease");
        _ = group.MapModulePost(MedicalApiRoutes.CodeSystemReleaseImport, ImportCodeSystemReleaseAsync, "MedicalImportCodeSystemRelease");
        _ = group.MapModulePost(MedicalApiRoutes.MedicalCodeSearch, SearchMedicalCodesAsync, "MedicalSearchMedicalCodes");
        _ = group.MapModulePost(MedicalApiRoutes.MedicalCodeResolve, ResolveMedicalCodeAsync, "MedicalResolveMedicalCode");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionReferenceCodeSearch, SearchPrescriptionReferenceCodesAsync, "MedicalSearchPrescriptionReferenceCodes");
        _ = group.MapModulePost(MedicalApiRoutes.DrugSearch, SearchDrugsAsync, "MedicalSearchDrugs");
        _ = group.MapModulePost(MedicalApiRoutes.DrugById, GetDrugAsync, "MedicalGetDrug");
        _ = group.MapModulePost(MedicalApiRoutes.DrugIdentifierResolve, ResolveDrugIdentifierAsync, "MedicalResolveDrugIdentifier");
        _ = group.MapModulePost(MedicalApiRoutes.DrugProductById, GetDrugProductAsync, "MedicalGetDrugProduct");
        _ = group.MapModulePost(MedicalApiRoutes.PackagedDrugById, GetPackagedDrugAsync, "MedicalGetPackagedDrug");
        _ = group.MapModulePost(MedicalApiRoutes.MedicalServiceSearch, SearchMedicalServicesAsync, "MedicalSearchMedicalServices");
        _ = group.MapModulePost(MedicalApiRoutes.MedicalServiceById, GetMedicalServiceAsync, "MedicalGetMedicalService");
        _ = group.MapModulePost(MedicalApiRoutes.ServiceCodeResolve, ResolveServiceCodeAsync, "MedicalResolveServiceCode");
        _ = group.MapModulePost(MedicalApiRoutes.ServiceValuation, GetServiceValuationAsync, "MedicalGetServiceValuation");
        _ = group.MapModulePost(MedicalApiRoutes.CodeMaps, GetCodeMapsAsync, "MedicalGetCodeMaps");
        _ = group.MapModulePost(MedicalApiRoutes.ImportById, GetImportBatchAsync, "MedicalGetImportBatch");
        _ = group.MapModulePost(MedicalApiRoutes.RegisterCodeSystem, RegisterCodeSystemAsync, "MedicalRegisterCodeSystem");
        _ = group.MapModulePost(MedicalApiRoutes.RegisterCodeSystemRelease, RegisterCodeSystemReleaseAsync, "MedicalRegisterCodeSystemRelease");
        _ = group.MapModulePost(MedicalApiRoutes.ActivateCodeSystemRelease, ActivateCodeSystemReleaseAsync, "MedicalActivateCodeSystemRelease");
        _ = group.MapModulePost(MedicalApiRoutes.DeactivateCodeSystemRelease, DeactivateCodeSystemReleaseAsync, "MedicalDeactivateCodeSystemRelease");
        _ = group.MapModulePost(MedicalApiRoutes.CreateLocalMedicalCode, CreateLocalMedicalCodeAsync, "MedicalCreateLocalMedicalCode");
        _ = group.MapModulePost(MedicalApiRoutes.CreateCodeMap, CreateCodeMapAsync, "MedicalCreateCodeMap");
        _ = group.MapModulePost(MedicalApiRoutes.CreateMedicalService, CreateMedicalServiceAsync, "MedicalCreateMedicalService");
        _ = group.MapModulePost(MedicalApiRoutes.UpdateLocalMedicalService, UpdateLocalMedicalServiceAsync, "MedicalUpdateLocalMedicalService");
        _ = group.MapModulePost(MedicalApiRoutes.RetireMedicalService, RetireMedicalServiceAsync, "MedicalRetireMedicalService");
        _ = group.MapModulePost(MedicalApiRoutes.CreateDrug, CreateDrugAsync, "MedicalCreateDrug");
        _ = group.MapModulePost(MedicalApiRoutes.UpdateLocalDrug, UpdateLocalDrugAsync, "MedicalUpdateLocalDrug");
        _ = group.MapModulePost(MedicalApiRoutes.RetireDrug, RetireDrugAsync, "MedicalRetireDrug");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionById, GetPrescriptionAsync, "MedicalGetPrescription");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionHistory, ListPatientPrescriptionsAsync, "MedicalListPatientPrescriptions");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionDrafts, CreatePrescriptionDraftAsync, "MedicalCreatePrescriptionDraft");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionDrafts + "/update", UpdatePrescriptionDraftAsync, "MedicalUpdatePrescriptionDraft");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionItems, AddPrescriptionItemAsync, "MedicalAddPrescriptionItem");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionItemById, UpdatePrescriptionItemAsync, "MedicalUpdatePrescriptionItem");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionItemById + "/remove", RemovePrescriptionItemAsync, "MedicalRemovePrescriptionItem");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionProviderSelection, SelectPrescriptionProviderAsync, "MedicalSelectPrescriptionProvider");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionEligibility, CheckPrescriptionEligibilityAsync, "MedicalCheckPrescriptionEligibility");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionDraftValidation, ValidatePrescriptionDraftAsync, "MedicalValidatePrescriptionDraft");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionSubmission, SubmitPrescriptionAsync, "MedicalSubmitPrescription");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionEdit, EditPrescriptionAsync, "MedicalEditPrescription");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionCancellation, CancelPrescriptionAsync, "MedicalCancelPrescription");
        _ = group.MapModulePost(MedicalApiRoutes.PaperFallback, StartPaperFallbackAsync, "MedicalStartPaperFallback");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplates + "/list", ListPrescriptionTemplatesAsync, "MedicalListPrescriptionTemplates");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplateById, GetPrescriptionTemplateAsync, "MedicalGetPrescriptionTemplate");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplates, SavePrescriptionTemplateAsync, "MedicalSavePrescriptionTemplate");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplateApply, ApplyPrescriptionTemplateAsync, "MedicalApplyPrescriptionTemplate");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplateArchive, ArchivePrescriptionTemplateAsync, "MedicalArchivePrescriptionTemplate");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplateItems, AddPrescriptionTemplateItemAsync, "MedicalAddPrescriptionTemplateItem");
        _ = group.MapModulePost(MedicalApiRoutes.PrescriptionTemplateItemRemove, RemovePrescriptionTemplateItemAsync, "MedicalRemovePrescriptionTemplateItem");
        _ = group.MapModulePost(MedicalApiRoutes.AvailableProviders, GetAvailableProvidersAsync, "MedicalGetAvailableProviders");
        _ = group.MapModulePost(MedicalApiRoutes.Credentials + "/list", ListProviderCredentialsAsync, "MedicalListProviderCredentials");
        _ = group.MapModulePost(MedicalApiRoutes.Credentials, ConfigureProviderCredentialAsync, "MedicalConfigureProviderCredential");
        _ = group.MapModulePost(MedicalApiRoutes.CredentialSetupGrid, GetCredentialSetupGridAsync, "MedicalGetCredentialSetupGrid");
        _ = group.MapModulePost(MedicalApiRoutes.CredentialTest, TestCredentialAsync, "MedicalTestCredential");
        _ = group.MapModulePost(MedicalApiRoutes.CredentialRevoke, RevokeCredentialAsync, "MedicalRevokeCredential");
        _ = group.MapModulePost(MedicalApiRoutes.ProviderReferenceDataSync, SyncProviderReferenceDataAsync, "MedicalSyncProviderReferenceData");
        _ = group.MapModulePost(MedicalApiRoutes.ObservabilityDashboard, GetPrescriptionOperationalDashboardAsync, "MedicalGetPrescriptionOperationalDashboard");
        _ = group.MapModulePost(MedicalApiRoutes.TransactionMetrics, GetProviderTransactionMetricsAsync, "MedicalGetProviderTransactionMetrics");
        _ = group.MapModulePost(MedicalApiRoutes.RejectionDistribution, GetRejectionDistributionAsync, "MedicalGetRejectionDistribution");
        _ = group.MapModulePost(MedicalApiRoutes.CredentialHealth, GetCredentialHealthAsync, "MedicalGetCredentialHealth");
        _ = group.MapModulePost(MedicalApiRoutes.DuplicateRiskIncidents, GetDuplicateRiskIncidentsAsync, "MedicalGetDuplicateRiskIncidents");
        _ = group.MapModulePost(MedicalApiRoutes.Reconciliation + "/queue", GetReconciliationQueueAsync, "MedicalGetReconciliationQueue");
        _ = group.MapModulePost(MedicalApiRoutes.OutboxStatus, GetOutboxStatusAsync, "MedicalGetOutboxStatus");
        _ = group.MapModulePost(MedicalApiRoutes.Reconciliation + "/refresh-provider-status", RefreshPrescriptionProviderStatusAsync, "MedicalRefreshPrescriptionProviderStatus");
        _ = group.MapModulePost(MedicalApiRoutes.Reconciliation + "/resolve-duplicate-risk", ResolveDuplicateRiskAsync, "MedicalResolveDuplicateRisk");
        _ = group.MapModulePost(MedicalApiRoutes.Reconciliation + "/paper-fallback", ReconcilePaperFallbackAsync, "MedicalReconcilePaperFallback");
        _ = group.MapModulePost(MedicalApiRoutes.Reconciliation + "/manual", ManualReconcileAsync, "MedicalManualReconcile");
        _ = group.MapModulePost(MedicalApiRoutes.Escalation, EscalatePrescriptionAsync, "MedicalEscalatePrescription");
        return endpoints;
    }

    private static async Task<IResult> ListCodeSystemsAsync(ListCodeSystemsRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.ListCodeSystemsAsync(request.ActiveOnly, ct)).Select(ToContract).ToArray());
    private static async Task<IResult> GetCodeSystemAsync(Guid codeSystemId, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetCodeSystemAsync(codeSystemId, ct)));
    private static async Task<IResult> GetCodeSystemByKeyAsync(GetCodeSystemByKeyRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetCodeSystemByKeyAsync(request.SystemKey, ct)));
    private static async Task<IResult> ListCodeSystemReleasesAsync(Guid codeSystemId, ListCodeSystemReleasesRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.ListReleasesAsync(codeSystemId, request.ActiveOnly, ct)).Select(ToContract).ToArray());
    private static async Task<IResult> GetActiveCodeSystemReleaseAsync(GetActiveCodeSystemReleaseRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetActiveReleaseAsync(request.SystemKey, request.AsOfDate, ct)));
    private static async Task<IResult> SearchMedicalCodesAsync(SearchMedicalCodesRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.SearchMedicalCodesAsync(request.SystemKey, request.TextOrCode, request.ReleaseId, request.ActiveOnly, request.AsOfDate, request.Limit, ct)).Select(ToSearchContract).ToArray());
    private static async Task<IResult> ResolveMedicalCodeAsync(ResolveMedicalCodeRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.ResolveMedicalCodeAsync(request.SystemKey, request.Code, request.ReleaseId, request.AsOfDate, ct)));
    private static async Task<IResult> SearchPrescriptionReferenceCodesAsync(SearchPrescriptionReferenceCodesRequest request, IMedicalQueryService service, CancellationToken ct) => await SearchMedicalCodesAsync(new SearchMedicalCodesRequest(request.Category.ToString(), request.TextOrCode, request.ReleaseId, request.ActiveOnly, request.AsOfDate, request.Limit), service, ct);
    private static async Task<IResult> SearchDrugsAsync(SearchDrugsRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.SearchDrugsAsync(request.Text, request.ActiveOnly, request.AsOfDate, request.Limit, ct)).Select(ToSearchContract).ToArray());
    private static async Task<IResult> GetDrugAsync(Guid drugId, GetDrugRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetDrugAsync(drugId, request.AsOfDate, ct)));
    private static async Task<IResult> ResolveDrugIdentifierAsync(ResolveDrugIdentifierRequest request, IMedicalQueryService service, CancellationToken ct)
    {
        if (!Enum.TryParse(request.IdentifierType, true, out DE.DrugIdentifierType identifierType))
        {
            return Results.BadRequest(new MedicalError(MedicalErrorCode.ValidationFailure, "Invalid drug identifier type."));
        }

        return OkOrNotFound(ToContract(await service.ResolveDrugIdentifierAsync(identifierType, request.IdentifierValue, request.AsOfDate, ct)));
    }
    private static async Task<IResult> GetDrugProductAsync(Guid drugProductId, GetDrugProductRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetDrugProductAsync(drugProductId, request.AsOfDate, ct)));
    private static async Task<IResult> GetPackagedDrugAsync(Guid packagedDrugId, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetPackagedDrugAsync(packagedDrugId, ct)));
    private static async Task<IResult> SearchMedicalServicesAsync(SearchMedicalServicesRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.SearchMedicalServicesAsync(request.Text, request.Category, request.Specialty, request.ActiveOnly, request.AsOfDate, request.Limit, ct)).Select(ToContract).ToArray());
    private static async Task<IResult> GetMedicalServiceAsync(Guid serviceId, GetMedicalServiceRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetMedicalServiceAsync(serviceId, request.AsOfDate, ct)));
    private static async Task<IResult> ResolveServiceCodeAsync(ResolveServiceCodeRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.ResolveServiceCodeAsync(request.SystemKey, request.Code, request.ReleaseId, request.AsOfDate, ct)));
    private static async Task<IResult> GetServiceValuationAsync(GetServiceValuationRequest request, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetServiceValuationAsync(request.ServiceCodeId, request.Year, request.Locality, request.FacilityIndicator is null ? null : Enum.Parse<DE.FacilityIndicator>(request.FacilityIndicator, true), request.AsOfDate, ct)));
    private static async Task<IResult> GetCodeMapsAsync(GetCodeMapsRequest request, IMedicalQueryService service, CancellationToken ct) => Ok((await service.GetCodeMapsAsync(request.SourceCodeId, request.TargetSystemKey, request.MappingType is null ? null : Enum.Parse<DE.MappingType>(request.MappingType.ToString(), true), request.AsOfDate, ct)).Select(ToContract).ToArray());
    private static async Task<IResult> GetImportBatchAsync(Guid importBatchId, IMedicalQueryService service, CancellationToken ct) => OkOrNotFound(ToContract(await service.GetImportBatchAsync(importBatchId, ct)));

    private static async Task<IResult> RegisterCodeSystemAsync(RegisterCodeSystemCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.RegisterCodeSystemCommand(request.SystemKey, request.DisplayName, request.Authority, (DE.CodeSystemCategory)request.Category, request.CanonicalUri, request.Description), ct));
    private static async Task<IResult> RegisterCodeSystemReleaseAsync(RegisterCodeSystemReleaseCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.RegisterCodeSystemReleaseCommand(request.CodeSystemId, request.Version, request.ReleaseDate, request.EffectiveFrom, request.EffectiveTo, request.SourceLocator, request.SourceVersion), ct));
    private static async Task<IResult> ActivateCodeSystemReleaseAsync(ActivateCodeSystemReleaseCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.ActivateCodeSystemReleaseCommand(request.ReleaseId), ct));
    private static async Task<IResult> DeactivateCodeSystemReleaseAsync(DeactivateCodeSystemReleaseCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.DeactivateCodeSystemReleaseCommand(request.ReleaseId), ct));
    private static async Task<IResult> ImportCodeSystemReleaseAsync(ImportCodeSystemReleaseRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.ImportCodeSystemReleaseCommand(request.CodeSystemId, request.Version, null, null, null, string.Empty, request.Version, null, (DE.ImportSourceType)request.SourceType, [.. request.Codes.Select(c => new A.ImportMedicalCodeItem(c.Code, c.DisplayName, c.Description, null, null, null))]), ct));
    private static async Task<IResult> CreateLocalMedicalCodeAsync(CreateLocalMedicalCodeCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.CreateLocalMedicalCodeCommand(request.CodeSystemReleaseId, request.Code, request.DisplayName, request.Description, request.EffectiveFrom, request.EffectiveTo), ct));
    private static async Task<IResult> CreateCodeMapAsync(CreateCodeMapCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.CreateCodeMapCommand(request.SourceMedicalCodeId, request.TargetMedicalCodeId, Enum.Parse<DE.MappingType>(request.MappingType.ToString(), true), 1.0m, null, null, request.Notes), ct));
    private static async Task<IResult> CreateMedicalServiceAsync(CreateMedicalServiceCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.CreateMedicalServiceCommand(request.Name, request.Category, request.Specialty, null, request.EffectiveFrom, request.EffectiveTo), ct));
    private static async Task<IResult> UpdateLocalMedicalServiceAsync(UpdateLocalMedicalServiceCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.UpdateLocalMedicalServiceCommand(request.ServiceId, request.Name, request.Category, request.Specialty, null, request.EffectiveFrom, request.EffectiveTo), ct));
    private static async Task<IResult> RetireMedicalServiceAsync(RetireMedicalServiceCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.RetireMedicalServiceCommand(request.ServiceId), ct));
    private static async Task<IResult> CreateDrugAsync(CreateDrugCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.CreateDrugCommand(request.PrimaryName, Enum.Parse<DE.DrugIdentifierType>(request.PrimaryIdentifierType.ToString(), true), request.PrimaryIdentifierValue ?? request.PrimaryName, request.EffectiveFrom, request.EffectiveTo), ct));
    private static async Task<IResult> UpdateLocalDrugAsync(UpdateLocalDrugCommand request, IMediator mediator, CancellationToken ct)
    {
        DE.DrugIdentifierType? identifierType = null;
        if (!string.IsNullOrWhiteSpace(request.PrimaryIdentifierType))
        {
            if (!Enum.TryParse(request.PrimaryIdentifierType, true, out DE.DrugIdentifierType parsedIdentifierType))
            {
                return Results.BadRequest(new MedicalError(MedicalErrorCode.ValidationFailure, "Invalid drug identifier type."));
            }

            identifierType = parsedIdentifierType;
        }

        return Command(await mediator.Send(new A.UpdateLocalDrugCommand(request.DrugId, request.PrimaryName, request.EffectiveFrom, request.EffectiveTo, identifierType, request.PrimaryIdentifierValue), ct));
    }
    private static async Task<IResult> RetireDrugAsync(RetireDrugCommand request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new A.RetireDrugCommand(request.DrugId), ct));

    private static async Task<IResult> GetPrescriptionAsync(Guid prescriptionId, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetPrescriptionQuery { PrescriptionId = prescriptionId }, ct));
    private static async Task<IResult> ListPatientPrescriptionsAsync(ListPatientPrescriptionsRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetPrescriptionHistoryQuery { PatientId = request.PatientId, ClinicId = request.ClinicId, StatusFilter = request.StatusFilter is null ? null : (DEP.PrescriptionStatus)request.StatusFilter, ProviderKeyFilter = request.ProviderKeyFilter, FromDate = request.FromDate, ToDate = request.ToDate, Skip = request.Skip, Take = request.Take }, ct));
    private static async Task<IResult> CreatePrescriptionDraftAsync(CreatePrescriptionDraftRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.CreateDraftCommand { PatientId = request.PatientId, PatientNationalId = request.PatientNationalId, DoctorId = request.DoctorId, ClinicId = request.ClinicId }, ct));
    private static Task<IResult> UpdatePrescriptionDraftAsync(UpdatePrescriptionDraftRequest request, CancellationToken ct) => Task.FromResult<IResult>(Ok(new MedicalCommandResult(request.PrescriptionId)));
    private static async Task<IResult> AddPrescriptionItemAsync(Guid prescriptionId, AddPrescriptionItemRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.AddItemCommand { PrescriptionId = prescriptionId, Category = (DEP.PrescriptionItemCategory)request.Category, ReferenceCode = request.ReferenceCode, DisplayName = request.DisplayName, DosageJson = request.DosageJson, Quantity = request.Quantity, DurationDays = request.DurationDays }, ct));
    private static Task<IResult> UpdatePrescriptionItemAsync(Guid prescriptionId, Guid itemId, UpdatePrescriptionItemRequest request, CancellationToken ct) => Task.FromResult<IResult>(Ok(new MedicalCommandResult(itemId)));
    private static async Task<IResult> RemovePrescriptionItemAsync(Guid prescriptionId, Guid itemId, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.RemoveItemCommand { PrescriptionId = prescriptionId, ItemId = itemId }, ct));
    private static async Task<IResult> SelectPrescriptionProviderAsync(Guid prescriptionId, SelectPrescriptionProviderRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.SetProviderCommand { PrescriptionId = prescriptionId, ProviderKey = request.ProviderKey }, ct));
    private static async Task<IResult> CheckPrescriptionEligibilityAsync(CheckPrescriptionEligibilityRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetEligibilityQuery { PatientNationalId = request.PatientNationalId, ProviderKey = request.ProviderKey, DoctorId = request.DoctorId, ClinicId = request.ClinicId }, ct));
    private static async Task<IResult> ValidatePrescriptionDraftAsync(Guid prescriptionId, IMediator mediator, CancellationToken ct) { var result = await mediator.Send(new AP.ValidateDraftCommand { PrescriptionId = prescriptionId }, ct); return Ok(new ValidationResultDto { IsValid = result.IsValid, Errors = result.Errors, Warnings = result.Warnings }); }
    private static async Task<IResult> SubmitPrescriptionAsync(Guid prescriptionId, SubmitPrescriptionRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.SubmitPrescriptionCommand { PrescriptionId = prescriptionId, CorrelationId = request.IdempotencyKey ?? Guid.NewGuid().ToString("N") }, ct));
    private static async Task<IResult> EditPrescriptionAsync(Guid prescriptionId, EditPrescriptionRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.EditPrescriptionCommand { PrescriptionId = prescriptionId, CorrelationId = request.IdempotencyKey ?? Guid.NewGuid().ToString("N"), EditReason = request.Reason }, ct));
    private static async Task<IResult> CancelPrescriptionAsync(Guid prescriptionId, CancelPrescriptionRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.CancelPrescriptionCommand { PrescriptionId = prescriptionId, CorrelationId = Guid.NewGuid().ToString("N"), Reason = request.Reason }, ct));
    private static async Task<IResult> StartPaperFallbackAsync(Guid prescriptionId, PaperFallbackRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.PaperFallbackCommand { PrescriptionId = prescriptionId, Reason = request.Reason, CorrelationId = Guid.NewGuid().ToString("N") }, ct));

    private static async Task<IResult> ListPrescriptionTemplatesAsync(ListPrescriptionTemplatesRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetPrescriptionTemplatesQuery { TenantId = request.TenantId ?? string.Empty, IncludeArchived = request.Status == MedicalPrescriptionTemplateStatus.Archived }, ct));
    private static async Task<IResult> GetPrescriptionTemplateAsync(Guid templateId, GetPrescriptionTemplateRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetPrescriptionTemplateQuery { TemplateId = templateId, TenantId = request.TenantId }, ct));
    private static async Task<IResult> SavePrescriptionTemplateAsync(SavePrescriptionTemplateRequest request, IMediator mediator, CancellationToken ct) => request.TemplateId is null ? Command(await mediator.Send(new AP.CreatePrescriptionTemplateCommand { TenantId = request.TenantId, Name = request.Name, Description = request.Description }, ct)) : Command(await mediator.Send(new AP.UpdatePrescriptionTemplateCommand { TemplateId = request.TemplateId.Value, TenantId = request.TenantId, Name = request.Name, Description = request.Description }, ct));
    private static async Task<IResult> ApplyPrescriptionTemplateAsync(Guid templateId, ApplyPrescriptionTemplateRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.LoadPrescriptionTemplateIntoDraftCommand { TemplateId = templateId, PrescriptionId = request.PrescriptionId, TenantId = request.TenantId }, ct));
    private static async Task<IResult> ArchivePrescriptionTemplateAsync(ArchivePrescriptionTemplateRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.ArchivePrescriptionTemplateCommand { TemplateId = request.TemplateId, TenantId = request.TenantId }, ct));
    private static async Task<IResult> AddPrescriptionTemplateItemAsync(AddPrescriptionTemplateItemRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.AddPrescriptionTemplateItemCommand { TemplateId = request.TemplateId, TenantId = request.TenantId, Category = (DEP.PrescriptionItemCategory)request.Category, ReferenceCode = request.ReferenceCode, DisplayName = request.DisplayName, Quantity = request.Quantity, DurationDays = request.DurationDays }, ct));
    private static async Task<IResult> RemovePrescriptionTemplateItemAsync(RemovePrescriptionTemplateItemRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.RemovePrescriptionTemplateItemCommand { TemplateId = request.TemplateId, ItemId = request.ItemId, TenantId = request.TenantId }, ct));
    private static async Task<IResult> GetAvailableProvidersAsync(GetAvailableProvidersRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetAvailableProvidersQuery(), ct));
    private static async Task<IResult> ListProviderCredentialsAsync(OperationalDashboardRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetCredentialHealthQuery { ProviderKey = request.ProviderKey, ClinicId = request.ClinicId }, ct));
    private static async Task<IResult> ConfigureProviderCredentialAsync(ConfigureProviderCredentialRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.SaveCredentialCommand { CredentialId = request.CredentialId, DoctorId = request.DoctorId, ClinicId = request.ClinicId, ProviderKey = request.ProviderKey }, ct));
    private static async Task<IResult> GetCredentialSetupGridAsync(GetCredentialSetupGridRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AP.GetCredentialSetupGridQuery { ClinicId = request.ClinicId, ProviderKey = request.ProviderKey }, ct));
    private static async Task<IResult> TestCredentialAsync(TestCredentialRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.TestCredentialCommand { CredentialId = request.CredentialId }, ct));
    private static async Task<IResult> RevokeCredentialAsync(RevokeCredentialRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.RevokeCredentialCommand { CredentialId = request.CredentialId }, ct));
    private static async Task<IResult> SyncProviderReferenceDataAsync(SyncProviderReferenceDataRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.SyncReferenceDataCommand { ProviderKey = request.ProviderKey, CodeSystem = request.CodeSystem }, ct));
    private static async Task<IResult> GetPrescriptionOperationalDashboardAsync(OperationalDashboardRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetOperationalAlertsQuery { ProviderKey = request.ProviderKey }, ct));
    private static async Task<IResult> GetProviderTransactionMetricsAsync(GetProviderTransactionMetricsRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetProviderTransactionMetricsQuery { ProviderKey = request.ProviderKey, From = request.From, To = request.To }, ct));
    private static async Task<IResult> GetRejectionDistributionAsync(GetRejectionDistributionRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetRejectionDistributionQuery { ProviderKey = request.ProviderKey, From = request.From, To = request.To }, ct));
    private static async Task<IResult> GetCredentialHealthAsync(GetCredentialHealthRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetCredentialHealthQuery { ProviderKey = request.ProviderKey, ClinicId = request.ClinicId }, ct));
    private static async Task<IResult> GetDuplicateRiskIncidentsAsync(GetDuplicateRiskIncidentsRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetDuplicateRiskIncidentsQuery { ProviderKey = request.ProviderKey, From = request.From, To = request.To, Take = request.Take }, ct));
    private static async Task<IResult> GetReconciliationQueueAsync(ReconciliationQueueRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetReconciliationQueueQuery { TenantId = request.TenantId, ProviderKey = request.ProviderKey, StatusFilter = request.StatusFilter is null ? null : (DEP.PrescriptionStatus)request.StatusFilter, Skip = request.Skip, Take = request.Take }, ct));
    private static async Task<IResult> GetOutboxStatusAsync(OutboxStatusRequest request, IMediator mediator, CancellationToken ct) => Ok(await mediator.Send(new AQ.GetOutboxStatusQuery { ProviderKey = request.ProviderKey, TenantId = request.TenantId }, ct));
    private static async Task<IResult> RefreshPrescriptionProviderStatusAsync(RefreshProviderStatusRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.RefreshProviderStatusCommand { PrescriptionId = request.PrescriptionId, CorrelationId = Guid.NewGuid().ToString("N") }, ct));
    private static async Task<IResult> ResolveDuplicateRiskAsync(ResolveDuplicateRiskRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.ResolveDuplicateRiskCommand { PrescriptionId = request.PrescriptionId, Resolution = AP.DuplicateRiskResolution.ConfirmedAccepted, Notes = request.ResolutionNotes, CorrelationId = Guid.NewGuid().ToString("N") }, ct));
    private static async Task<IResult> ReconcilePaperFallbackAsync(ReconcilePaperFallbackRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.ReconcilePaperFallbackCommand { PrescriptionId = request.PrescriptionId, RemotePrescriptionId = request.PaperTrackingNumber, CorrelationId = Guid.NewGuid().ToString("N") }, ct));
    private static async Task<IResult> ManualReconcileAsync(ManualReconcileRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.ManualReconcileCommand { PrescriptionId = request.PrescriptionId, Notes = request.Notes, CorrelationId = Guid.NewGuid().ToString("N") }, ct));
    private static async Task<IResult> EscalatePrescriptionAsync(EscalatePrescriptionRequest request, IMediator mediator, CancellationToken ct) => Command(await mediator.Send(new AP.EscalateCommand { PrescriptionId = request.PrescriptionId, OperatorId = Guid.Empty, Summary = request.Reason, CorrelationId = Guid.NewGuid().ToString("N") }, ct));

    private static IResult Ok<T>(T value) => Results.Ok(value);
    private static IResult OkOrNotFound<T>(T? value) => value is null ? Results.NotFound() : Results.Ok(value);
    private static IResult Command(dynamic? result)
    {
        if (result is null)
        {
            return Results.Ok(new MedicalCommandResult(Accepted: false, UserSafeMessage: "No result returned."));
        }

        Type type = result.GetType();
        Guid? id = type.GetProperty("EntityId")?.GetValue(result) as Guid?
            ?? type.GetProperty("PrescriptionId")?.GetValue(result) as Guid?
            ?? type.GetProperty("ItemId")?.GetValue(result) as Guid?
            ?? type.GetProperty("CredentialId")?.GetValue(result) as Guid?
            ?? type.GetProperty("TemplateId")?.GetValue(result) as Guid?;
        bool isSuccess = type.GetProperty("IsSuccess")?.GetValue(result) as bool? ?? true;
        string? errorMessage = type.GetProperty("ErrorMessage")?.GetValue(result) as string;
        return Results.Ok(new MedicalCommandResult(id, isSuccess, errorMessage));
    }
    private static CodeSystemResponse? ToContract(D.CodeSystem? e) => e is null ? null : new() { Id = e.Id, SystemKey = e.SystemKey, Name = e.Name, Category = (MedicalCodeSystemCategory)e.Category, IsActive = e.IsActive, CanRetire = true };
    private static CodeSystemReleaseResponse? ToContract(D.CodeSystemRelease? e) => e is null ? null : new() { Id = e.Id, CodeSystemId = e.CodeSystemId, Version = e.SourceVersion ?? e.ReleaseName, EffectiveFrom = e.EffectiveFrom ?? DateOnly.MinValue, EffectiveTo = e.EffectiveTo, IsActive = e.IsActive };
    private static MedicalCodeResponse? ToContract(D.MedicalCode? e) => e is null ? null : new() { Id = e.Id, CodeSystemReleaseId = e.CodeSystemReleaseId, Code = e.Code, DisplayName = e.DisplayName, Description = e.LongDescription, Status = (MedicalCodeStatus)e.Status };
    private static MedicalCodeSearchResultResponse ToSearchContract(D.MedicalCode e) => new() { Id = e.Id, Code = e.Code, DisplayName = e.DisplayName, Status = (MedicalCodeStatus)e.Status, CanRetire = true };
    private static DrugResponse? ToContract(D.Drug? e) => e is null ? null : new() { Id = e.Id, PrimaryName = e.PrimaryName, PrimaryIdentifierType = e.PrimaryIdentifierType.ToString(), PrimaryIdentifierValue = e.PrimaryIdentifierValue, Status = (DrugStatus)e.Status };
    private static DrugSearchResultResponse ToSearchContract(D.Drug e) => new() { Id = e.Id, PrimaryName = e.PrimaryName, PrimaryIdentifierType = e.PrimaryIdentifierType.ToString(), PrimaryIdentifierValue = e.PrimaryIdentifierValue, Status = (DrugStatus)e.Status, CanRetire = true };
    private static DrugProductResponse? ToContract(D.DrugProduct? e) => e is null ? null : new() { Id = e.Id, DrugId = e.DrugId, BrandName = e.BrandName, GenericName = e.GenericName, DoseForm = e.DoseForm, Route = e.Route, Strength = e.Strength, Manufacturer = e.Manufacturer, MarketStatus = e.MarketStatus.ToString() };
    private static PackagedDrugResponse? ToContract(D.PackagedDrug? e) => e is null ? null : new() { Id = e.Id, DrugProductId = e.DrugProductId, PackageIdentifier = e.PackageIdentifier, IdentifierSource = e.IdentifierSource.ToString(), NdcCode = e.NdcCode, PackageSize = e.PackageSize, PackageUnit = e.PackageUnit, PackageDescription = e.PackageDescription };
    private static MedicalServiceResponse? ToContract(D.MedicalService? e) => e is null ? null : new() { Id = e.Id, Name = e.Name, Category = e.Category, Specialty = e.Specialty, Status = (MedicalServiceStatus)e.Status, CanRetire = true };
    private static ServiceCodeResponse? ToContract(D.ServiceCode? e) => e is null ? null : new() { Id = e.Id, MedicalServiceId = e.MedicalServiceId, CodeSystemReleaseId = e.CodeSystemReleaseId, Code = e.Code, DisplayName = e.DisplayName, IsPrimary = e.IsPrimary, Status = e.Status.ToString() };
    private static ServiceValuationResponse? ToContract(D.ServiceValuation? e) => e is null ? null : new() { Id = e.Id, ServiceCodeId = e.ServiceCodeId, Year = e.ValuationYear, Locality = e.Locality, Amount = e.ConversionFactor };
    private static CodeMapResponse ToContract(D.CodeMap e) => new() { Id = e.Id, SourceMedicalCodeId = e.SourceMedicalCodeId, TargetMedicalCodeId = e.TargetMedicalCodeId, Status = (MedicalCodeMapStatus)e.Status };
    private static ImportBatchResponse? ToContract(D.ImportBatch? e) => e is null ? null : new() { Id = e.Id, Status = (MedicalImportBatchStatus)e.Status, CreatedAt = e.CreatedAt, CompletedAt = e.CompletedAt, Errors = string.IsNullOrWhiteSpace(e.ErrorReport) ? [] : [e.ErrorReport], Warnings = string.IsNullOrWhiteSpace(e.ValidationSummary) ? [] : [e.ValidationSummary] };
    public static IResult ToHttpFailure(MedicalError error) => error.Code switch { MedicalErrorCode.ValidationFailure => Results.BadRequest(error), MedicalErrorCode.Unauthorized => Results.Unauthorized(), MedicalErrorCode.Forbidden => Results.Forbid(), MedicalErrorCode.NotFound => Results.NotFound(error), MedicalErrorCode.Conflict => Results.Conflict(error), MedicalErrorCode.ProviderUnavailable => Results.StatusCode(StatusCodes.Status503ServiceUnavailable), MedicalErrorCode.Timeout => Results.StatusCode(StatusCodes.Status504GatewayTimeout), MedicalErrorCode.ProviderRejected => Results.UnprocessableEntity(error), MedicalErrorCode.CredentialInvalid or MedicalErrorCode.CredentialExpired or MedicalErrorCode.CredentialRevoked => Results.BadRequest(error), _ => Results.Problem(error.Message) };
}
