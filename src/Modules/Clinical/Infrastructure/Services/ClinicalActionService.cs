using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalActionService(
    ClinicalDbContext dbContext,
    IWorkflowPolicyEngine policyEngine,
    IValidationCoordinator validationCoordinator,
    IFhirResourceMapper resourceMapper,
    IClinicalOperationsService operationsService,
    IVisitWorkflowService workflowService,
    IClinicalResourceLinkService resourceLinkService,
    IWorkflowEventPublisher eventPublisher,
    IConflictHandler conflictHandler,
    IClinicalAuditWriter auditWriter,
    IRequestContext requestContext) : IClinicalActionService
{
    public async Task<ClinicalActionResult> DispatchAsync(
        ClinicalActionRequest request,
        Guid? workflowId = null,
        CancellationToken cancellationToken = default)
    {
        string correlationId = request.CorrelationId ?? Guid.NewGuid().ToString("N");

        // Load workflow if present
        ClinicalWorkflow? workflow = null;
        if (workflowId.HasValue)
        {
            VisitWorkflowReadDto? dto = await workflowService.GetAsync(workflowId.Value, cancellationToken);
            if (dto is null)
            {
                return ClinicalActionResult.Fail($"Workflow '{workflowId}' not found.");
            }

            workflow = await dbContext.ClinicalWorkflows
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == workflowId.Value, cancellationToken);
        }

        // Load policy
        WorkflowPolicyDefinition policy = await LoadPolicyAsync(request.TenantId, workflow?.VisitType, cancellationToken);

        // Resolve actor roles
        IReadOnlyList<string> actorRoles = request is { Payload: null } ? [] : ResolveRoles(requestContext);

        // Policy engine evaluation
        IReadOnlyList<WorkflowValidationResult> policyErrors = await policyEngine.EvaluateAsync(
            request.ActorId,
            actorRoles,
            request.ActionType,
            workflow,
            policy,
            request.Payload,
            cancellationToken);

        IReadOnlyList<WorkflowValidationResult> hardErrors = policyErrors
            .Where(e => e.Severity == WorkflowValidationSeverity.Error)
            .ToArray();

        if (hardErrors.Count > 0)
        {
            await WriteActionRecordAsync(workflowId, request, "Failed", "Failed", null, null, JsonSerializer.Serialize(hardErrors), cancellationToken);
            return ClinicalActionResult.Fail(hardErrors);
        }

        // Duplicate check
        if (workflowId.HasValue && !conflictHandler.IsRetriableSafe(request.ActionType))
        {
            bool isDuplicate = await conflictHandler.IsDuplicateActionAsync(workflowId.Value, request.ActionType, cancellationToken: cancellationToken);
            if (isDuplicate)
            {
                return ClinicalActionResult.Fail([], $"Duplicate action '{request.ActionType}' detected for workflow '{workflowId}'.");
            }
        }

        // Coordination validation
        if (request.Payload is not null)
        {
            IReadOnlyList<WorkflowValidationResult> coordErrors = await validationCoordinator.ValidateAsync(
                request.ActionType,
                request.Payload,
                policy,
                request.PatientFhirReference,
                request.EncounterFhirReference,
                cancellationToken);

            IReadOnlyList<WorkflowValidationResult> coordHardErrors = coordErrors
                .Where(e => e.Severity == WorkflowValidationSeverity.Error)
                .ToArray();

            if (coordHardErrors.Count > 0)
            {
                await WriteActionRecordAsync(workflowId, request, "Failed", "Failed", null, null, JsonSerializer.Serialize(coordHardErrors), cancellationToken);
                return ClinicalActionResult.Fail(coordHardErrors);
            }
        }

        // Actions that do not require FHIR writes
        if (IsStateOnlyAction(request.ActionType) && workflowId.HasValue)
        {
            return await HandleStateOnlyActionAsync(request, workflowId.Value, correlationId, cancellationToken);
        }

        // Map FHIR resources
        if (request.Payload is null)
        {
            return ClinicalActionResult.Fail("Payload is required for this action.");
        }

        IReadOnlyList<FhirMappedResource> mappedResources = await resourceMapper.MapAsync(
            request.ActionType,
            request.Payload,
            request.PatientFhirReference,
            request.EncounterFhirReference,
            cancellationToken);

        if (mappedResources.Count == 0)
        {
            return ClinicalActionResult.Fail("Resource mapper produced no resources for this action.");
        }

        // Commit to FHIR
        Guid actionRecordId = Guid.NewGuid();
        List<string> createdRefs = [];
        string? bundleRef = null;

        try
        {
            if (mappedResources.Count > 1)
            {
                // Use transaction bundle
                FhirMappedResource bundle = Vitarish.Clinical.Infrastructure.Mapping.FhirTransactionBundleBuilder.Build(mappedResources);
                FhirOperationResponse bundleResponse = await operationsService.ExecuteAsync(new FhirOperationRequest
                {
                    ResourceType = "Bundle",
                    Interaction = FhirInteractionType.Create,
                    PayloadJson = bundle.ResourceJson,
                    CorrelationId = correlationId,
                    AccessReason = request.AccessReason
                }, cancellationToken);

                if (bundleResponse.Status != FhirOperationStatus.Succeeded)
                {
                    await WriteActionRecordAsync(workflowId, request, "Passed", "Failed", null, bundleResponse.FailureMessage, null, cancellationToken);
                    await PublishCommitFailedAsync(workflowId, request, bundleResponse, correlationId, cancellationToken);
                    return ClinicalActionResult.Fail(bundleResponse.FailureMessage ?? "FHIR transaction failed.");
                }

                bundleRef = $"Bundle/{bundleResponse.OperationId}";
            }
            else
            {
                FhirMappedResource mapped = mappedResources[0];
                FhirOperationResponse fhirResponse = await operationsService.ExecuteAsync(new FhirOperationRequest
                {
                    ResourceType = mapped.ResourceType,
                    ResourceId = mapped.ResourceId,
                    Interaction = string.IsNullOrWhiteSpace(mapped.ResourceId) ? FhirInteractionType.Create : FhirInteractionType.Update,
                    PayloadJson = mapped.ResourceJson,
                    CorrelationId = correlationId,
                    AccessReason = request.AccessReason
                }, cancellationToken);

                if (fhirResponse.Status != FhirOperationStatus.Succeeded)
                {
                    await WriteActionRecordAsync(workflowId, request, "Passed", "Failed", null, fhirResponse.FailureMessage, null, cancellationToken);
                    await PublishCommitFailedAsync(workflowId, request, fhirResponse, correlationId, cancellationToken);
                    return ClinicalActionResult.Fail(fhirResponse.FailureMessage ?? "FHIR commit failed.");
                }

                string resourceRef = $"{mapped.ResourceType}/{fhirResponse.OperationId}";
                createdRefs.Add(resourceRef);

                // Register resource link
                if (workflowId.HasValue)
                {
                    await resourceLinkService.LinkAsync(new ResourceLinkWriteDto
                    {
                        LocalEntityType = "ClinicalWorkflow",
                        LocalEntityId = workflowId.Value,
                        ResourceType = mapped.ResourceType,
                        ResourceId = fhirResponse.OperationId.ToString(),
                        ResourceVersionId = fhirResponse.RemoteVersionId,
                        PatientReference = request.PatientFhirReference
                    }, cancellationToken);
                }

                // Update workflow encounter reference if this was StartEncounter
                if (request.ActionType == ClinicalActionType.StartEncounter && workflowId.HasValue)
                {
                    Domain.Entities.ClinicalWorkflow? wf = await dbContext.ClinicalWorkflows
                        .FirstOrDefaultAsync(w => w.Id == workflowId.Value, cancellationToken);

                    if (wf is not null)
                    {
                        wf.EncounterFhirReference = resourceRef;
                        await dbContext.SaveChangesAsync(cancellationToken);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await WriteActionRecordAsync(workflowId, request, "Passed", "Failed", null, ex.Message, null, cancellationToken);
            await eventPublisher.PublishFhirCommitFailedAsync(
                workflowId ?? Guid.Empty,
                request.ActionType.ToString(),
                "Unexpected",
                correlationId,
                cancellationToken);
            return ClinicalActionResult.Fail(ex.Message);
        }

        // Record action
        ClinicalActionRecord actionRecord = await WriteActionRecordAsync(
            workflowId, request, "Passed", "Succeeded", bundleRef ?? (createdRefs.Count > 0 ? createdRefs[0] : null), null, null, cancellationToken);

        // Advance workflow state
        if (workflowId.HasValue)
        {
            await AdvanceWorkflowStateAsync(request, workflowId.Value, cancellationToken);
        }

        // Publish success events
        await PublishSuccessEventAsync(request, createdRefs, bundleRef, correlationId, workflowId, cancellationToken);

        return ClinicalActionResult.Success(
            fhirRefsCreated: createdRefs,
            fhirTransactionReference: bundleRef,
            actionRecord: ToRecordDto(actionRecord));
    }

    public async Task<IReadOnlyList<ClinicalActionRecordReadDto>> ListRecordsAsync(
        ClinicalActionRecordFilter filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ClinicalActionRecord> query = dbContext.ClinicalActionRecords.AsNoTracking();

        if (filter.WorkflowId.HasValue)
        {
            query = query.Where(r => r.WorkflowId == filter.WorkflowId.Value);
        }

        if (filter.ActionType.HasValue)
        {
            query = query.Where(r => r.ActionType == filter.ActionType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.CommitStatus))
        {
            string status = filter.CommitStatus.Trim();
            query = query.Where(r => r.CommitStatus == status);
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        List<ClinicalActionRecord> records = await query
            .OrderByDescending(r => r.OccurredAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return records.Select(ToRecordDto).ToArray();
    }

    private async Task<ClinicalActionResult> HandleStateOnlyActionAsync(
        ClinicalActionRequest request,
        Guid workflowId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        VisitWorkflowStatus? targetStatus = request.ActionType switch
        {
            ClinicalActionType.CheckInPatient => VisitWorkflowStatus.CheckedIn,
            ClinicalActionType.CancelVisit => VisitWorkflowStatus.Cancelled,
            ClinicalActionType.ReopenVisit => VisitWorkflowStatus.Draft,
            ClinicalActionType.CompleteVisit => VisitWorkflowStatus.Completed,
            _ => null
        };

        if (targetStatus.HasValue)
        {
            await workflowService.TransitionAsync(workflowId, targetStatus.Value, request.ActorId, null, cancellationToken);
        }

        ClinicalActionRecord record = await WriteActionRecordAsync(workflowId, request, "Passed", "Succeeded", null, null, null, cancellationToken);
        return ClinicalActionResult.Success([], actionRecord: ToRecordDto(record));
    }

    private async Task AdvanceWorkflowStateAsync(ClinicalActionRequest request, Guid workflowId, CancellationToken cancellationToken)
    {
        VisitWorkflowStatus? target = request.ActionType switch
        {
            ClinicalActionType.RecordVitals => VisitWorkflowStatus.ReadyForProvider,
            ClinicalActionType.StartEncounter => VisitWorkflowStatus.InProgress,
            _ => null
        };

        if (target.HasValue)
        {
            try
            {
                await workflowService.TransitionAsync(workflowId, target.Value, request.ActorId, null, cancellationToken);
            }
            catch
            {
                // Best effort — don't fail the action if state advance fails
            }
        }
    }

    private async Task PublishSuccessEventAsync(ClinicalActionRequest request, List<string> createdRefs, string? bundleRef, string correlationId, Guid? workflowId, CancellationToken cancellationToken)
    {
        switch (request.ActionType)
        {
            case ClinicalActionType.RegisterPatient when createdRefs.Count > 0:
                await eventPublisher.PublishPatientRegisteredAsync(createdRefs[0], request.ActorId, request.TenantId, cancellationToken);
                break;
            case ClinicalActionType.RecordVitals:
                await eventPublisher.PublishVitalsRecordedAsync(workflowId ?? Guid.Empty, createdRefs, request.ActorId, cancellationToken);
                break;
            case ClinicalActionType.AddDiagnosis when createdRefs.Count > 0:
                await eventPublisher.PublishDiagnosisAddedAsync(workflowId ?? Guid.Empty, createdRefs[0], request.ActorId, cancellationToken);
                break;
            case ClinicalActionType.AddMedicationRequest when createdRefs.Count > 0:
                await eventPublisher.PublishMedicationRequestedAsync(workflowId ?? Guid.Empty, createdRefs[0], request.ActorId, cancellationToken);
                break;
            case ClinicalActionType.SignNote when createdRefs.Count > 0:
                await eventPublisher.PublishClinicalNoteSignedAsync(workflowId ?? Guid.Empty, createdRefs[0], request.ActorId, cancellationToken);
                break;
        }
    }

    private async Task PublishCommitFailedAsync(Guid? workflowId, ClinicalActionRequest request, FhirOperationResponse response, string correlationId, CancellationToken cancellationToken)
    {
        await eventPublisher.PublishFhirCommitFailedAsync(
            workflowId ?? Guid.Empty,
            request.ActionType.ToString(),
            response.FailureKind.ToString(),
            correlationId,
            cancellationToken);
    }

    private async Task<ClinicalActionRecord> WriteActionRecordAsync(
        Guid? workflowId,
        ClinicalActionRequest request,
        string validationStatus,
        string commitStatus,
        string? fhirRef,
        string? errorMessage,
        string? errorDetailsJson,
        CancellationToken cancellationToken)
    {
        ClinicalActionRecord record = new()
        {
            WorkflowId = workflowId ?? Guid.Empty,
            ActionType = request.ActionType,
            ActorId = request.ActorId,
            RequestJson = request.Payload is not null ? JsonSerializer.Serialize(request.Payload) : null,
            ValidationStatus = validationStatus,
            CommitStatus = commitStatus,
            FhirTransactionReference = fhirRef,
            ErrorDetailsJson = errorDetailsJson ?? (errorMessage is not null ? JsonSerializer.Serialize(new { message = errorMessage }) : null),
            OccurredAt = DateTimeOffset.UtcNow,
            CreatedBy = request.ActorId
        };

        dbContext.ClinicalActionRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditWriter.WriteAsync(new AuditWriteRequest
        {
            Category = "ClinicalAction",
            Action = request.ActionType.ToString(),
            ActorId = request.ActorId,
            ResourceType = "ClinicalWorkflow",
            ResourceId = workflowId?.ToString(),
            CorrelationId = request.CorrelationId,
            Reason = request.AccessReason,
            Outcome = commitStatus
        }, cancellationToken);

        return record;
    }

    private async Task<WorkflowPolicyDefinition> LoadPolicyAsync(string tenantId, string? visitType, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(visitType))
        {
            return WorkflowPolicyDefinition.CreateDefault();
        }

        WorkflowPolicy? policy = await dbContext.WorkflowPolicies
            .AsNoTracking()
            .Where(p => p.VisitType == visitType && p.IsActive)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (policy is null || string.IsNullOrWhiteSpace(policy.PolicyJson))
        {
            return WorkflowPolicyDefinition.CreateDefault();
        }

        try
        {
            return JsonSerializer.Deserialize<WorkflowPolicyDefinition>(policy.PolicyJson)
                ?? WorkflowPolicyDefinition.CreateDefault();
        }
        catch
        {
            return WorkflowPolicyDefinition.CreateDefault();
        }
    }

    private static IReadOnlyList<string> ResolveRoles(IRequestContext requestContext)
    {
        System.Security.Claims.ClaimsPrincipal? user = requestContext.User;
        if (user is null)
        {
            return [];
        }

        return user.Claims
            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();
    }

    private static bool IsStateOnlyAction(ClinicalActionType actionType) => actionType is
        ClinicalActionType.CheckInPatient or
        ClinicalActionType.CancelVisit or
        ClinicalActionType.ReopenVisit or
        ClinicalActionType.CompleteVisit;

    private static ClinicalActionRecordReadDto ToRecordDto(ClinicalActionRecord r) => new()
    {
        Id = r.Id,
        WorkflowId = r.WorkflowId,
        ActionType = r.ActionType,
        ActorId = r.ActorId,
        ValidationStatus = r.ValidationStatus,
        CommitStatus = r.CommitStatus,
        FhirTransactionReference = r.FhirTransactionReference,
        ErrorDetailsJson = r.ErrorDetailsJson,
        OccurredAt = r.OccurredAt
    };
}
