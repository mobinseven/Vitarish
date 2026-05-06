using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class VisitWorkflowService(
    ClinicalDbContext dbContext,
    IWorkflowEventPublisher eventPublisher,
    IRequestContext requestContext) : IVisitWorkflowService
{
    public async Task<VisitWorkflowReadDto> CreateAsync(VisitWorkflowWriteDto writeDto, string actorId, CancellationToken cancellationToken = default)
    {
        ClinicalWorkflow workflow = new()
        {
            PatientFhirReference = writeDto.PatientFhirReference,
            EncounterFhirReference = null,
            AppointmentFhirReference = writeDto.AppointmentFhirReference,
            VisitType = writeDto.VisitType,
            AssignedProviderId = writeDto.AssignedProviderId,
            AssignedLocationId = writeDto.AssignedLocationId,
            Status = VisitWorkflowStatus.Draft,
            WorkflowPolicyId = writeDto.WorkflowPolicyId,
            CreatedBy = actorId
        };

        dbContext.ClinicalWorkflows.Add(workflow);
        await dbContext.SaveChangesAsync(cancellationToken);

        string tenantId = requestContext.TenantId ?? string.Empty;
        await eventPublisher.PublishVisitWorkflowCreatedAsync(workflow.Id, workflow.PatientFhirReference, workflow.VisitType, tenantId, cancellationToken);

        return ToDto(workflow);
    }

    public async Task<VisitWorkflowReadDto?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        ClinicalWorkflow? workflow = await dbContext.ClinicalWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        return workflow is null ? null : ToDto(workflow);
    }

    public async Task<IReadOnlyList<VisitWorkflowReadDto>> ListAsync(VisitWorkflowFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<ClinicalWorkflow> query = dbContext.ClinicalWorkflows.AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(w => w.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedLocationId))
        {
            string locationId = filter.AssignedLocationId.Trim();
            query = query.Where(w => w.AssignedLocationId == locationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.AssignedProviderId))
        {
            string providerId = filter.AssignedProviderId.Trim();
            query = query.Where(w => w.AssignedProviderId == providerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.PatientFhirReference))
        {
            string patientRef = filter.PatientFhirReference.Trim();
            query = query.Where(w => w.PatientFhirReference == patientRef);
        }

        if (!string.IsNullOrWhiteSpace(filter.VisitType))
        {
            string visitType = filter.VisitType.Trim();
            query = query.Where(w => w.VisitType == visitType);
        }

        if (filter.Since.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= filter.Since.Value);
        }

        if (filter.Until.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= filter.Until.Value);
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        List<ClinicalWorkflow> results = await query
            .OrderByDescending(w => w.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return results.Select(ToDto).ToArray();
    }

    public async Task<VisitWorkflowReadDto> TransitionAsync(Guid workflowId, VisitWorkflowStatus targetStatus, string actorId, string? reason = null, CancellationToken cancellationToken = default)
    {
        ClinicalWorkflow workflow = await RequireWorkflowAsync(workflowId, cancellationToken);
        VisitWorkflowStatus previousStatus = workflow.Status;

        workflow.Status = targetStatus;
        workflow.UpdatedBy = actorId;

        WorkflowStateHistory history = new()
        {
            WorkflowId = workflow.Id,
            PreviousStatus = previousStatus,
            NewStatus = targetStatus,
            ActorId = actorId,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow
        };

        dbContext.WorkflowStateHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        await PublishTransitionEventAsync(workflow, targetStatus, actorId, cancellationToken);

        return ToDto(workflow);
    }

    public async Task<VisitWorkflowReadDto> CancelAsync(Guid workflowId, string actorId, string reason, CancellationToken cancellationToken = default)
    {
        ClinicalWorkflow workflow = await RequireWorkflowAsync(workflowId, cancellationToken);
        VisitWorkflowStatus previousStatus = workflow.Status;

        workflow.Status = VisitWorkflowStatus.Cancelled;
        workflow.UpdatedBy = actorId;

        WorkflowStateHistory history = new()
        {
            WorkflowId = workflow.Id,
            PreviousStatus = previousStatus,
            NewStatus = VisitWorkflowStatus.Cancelled,
            ActorId = actorId,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow
        };

        dbContext.WorkflowStateHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        string tenantId = requestContext.TenantId ?? string.Empty;
        await eventPublisher.PublishWorkflowCancelledAsync(workflow.Id, reason, actorId, tenantId, cancellationToken);

        return ToDto(workflow);
    }

    public async Task<VisitWorkflowReadDto> ReopenAsync(Guid workflowId, string actorId, string reason, CancellationToken cancellationToken = default)
    {
        ClinicalWorkflow workflow = await RequireWorkflowAsync(workflowId, cancellationToken);
        VisitWorkflowStatus previousStatus = workflow.Status;

        workflow.Status = VisitWorkflowStatus.Draft;
        workflow.UpdatedBy = actorId;

        WorkflowStateHistory history = new()
        {
            WorkflowId = workflow.Id,
            PreviousStatus = previousStatus,
            NewStatus = VisitWorkflowStatus.Draft,
            ActorId = actorId,
            Reason = reason,
            OccurredAt = DateTimeOffset.UtcNow
        };

        dbContext.WorkflowStateHistories.Add(history);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(workflow);
    }

    public async Task<IReadOnlyList<OperationalProjectionSummary>> GetQueueAsync(string projectionType, string? locationId = null, CancellationToken cancellationToken = default)
    {
        VisitWorkflowStatus[] statuses = projectionType switch
        {
            "nurse" => [VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage],
            "provider" => [VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress],
            "incomplete" => [VisitWorkflowStatus.Draft, VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage, VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress, VisitWorkflowStatus.Signed],
            _ => [VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage, VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress]
        };

        IQueryable<ClinicalWorkflow> query = dbContext.ClinicalWorkflows
            .AsNoTracking()
            .Where(w => statuses.Contains(w.Status));

        if (!string.IsNullOrWhiteSpace(locationId))
        {
            string loc = locationId.Trim();
            query = query.Where(w => w.AssignedLocationId == loc);
        }

        List<ClinicalWorkflow> workflows = await query
            .OrderBy(w => w.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return workflows.Select(w => new OperationalProjectionSummary
        {
            WorkflowId = w.Id,
            PatientFhirReference = w.PatientFhirReference,
            EncounterFhirReference = w.EncounterFhirReference,
            VisitType = w.VisitType,
            Status = w.Status,
            AssignedProviderId = w.AssignedProviderId,
            AssignedLocationId = w.AssignedLocationId,
            WorkflowCreatedAt = w.CreatedAt,
            ProjectionType = projectionType
        }).ToArray();
    }

    private async Task<ClinicalWorkflow> RequireWorkflowAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        ClinicalWorkflow? workflow = await dbContext.ClinicalWorkflows
            .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

        return workflow ?? throw new InvalidOperationException($"Visit workflow '{workflowId}' was not found.");
    }

    private async Task PublishTransitionEventAsync(ClinicalWorkflow workflow, VisitWorkflowStatus newStatus, string actorId, CancellationToken cancellationToken)
    {
        string tenantId = requestContext.TenantId ?? string.Empty;

        switch (newStatus)
        {
            case VisitWorkflowStatus.CheckedIn:
                await eventPublisher.PublishVisitCheckedInAsync(workflow.Id, workflow.PatientFhirReference, actorId, cancellationToken);
                break;
            case VisitWorkflowStatus.InProgress when workflow.EncounterFhirReference is not null:
                await eventPublisher.PublishEncounterStartedAsync(workflow.Id, workflow.EncounterFhirReference, workflow.PatientFhirReference, cancellationToken);
                break;
            case VisitWorkflowStatus.Completed when workflow.EncounterFhirReference is not null:
                await eventPublisher.PublishVisitCompletedAsync(workflow.Id, workflow.EncounterFhirReference, workflow.PatientFhirReference, tenantId, cancellationToken);
                break;
        }
    }

    private static VisitWorkflowReadDto ToDto(ClinicalWorkflow w) => new()
    {
        Id = w.Id,
        PatientFhirReference = w.PatientFhirReference,
        EncounterFhirReference = w.EncounterFhirReference,
        AppointmentFhirReference = w.AppointmentFhirReference,
        VisitType = w.VisitType,
        AssignedProviderId = w.AssignedProviderId,
        AssignedLocationId = w.AssignedLocationId,
        Status = w.Status,
        WorkflowPolicyId = w.WorkflowPolicyId,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt,
        CreatedBy = w.CreatedBy
    };
}
