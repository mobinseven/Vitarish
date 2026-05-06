using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ProjectionBuilderService(ClinicalDbContext dbContext) : IProjectionBuilder
{
    public Task RefreshNurseQueueAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RefreshProviderWorklistAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RefreshIncompleteVisitsAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public async Task<IReadOnlyList<OperationalProjectionSummary>> GetQueueAsync(
        string projectionType,
        string? locationId = null,
        CancellationToken cancellationToken = default)
    {
        VisitWorkflowStatus[] statuses = projectionType switch
        {
            "nurse" => [VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage],
            "provider" => [VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress],
            "incomplete" => [VisitWorkflowStatus.Draft, VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage,
                VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress, VisitWorkflowStatus.Signed],
            _ => [VisitWorkflowStatus.CheckedIn, VisitWorkflowStatus.InTriage,
                VisitWorkflowStatus.ReadyForProvider, VisitWorkflowStatus.InProgress]
        };

        IQueryable<Domain.Entities.ClinicalWorkflow> query = dbContext.ClinicalWorkflows
            .AsNoTracking()
            .Where(w => statuses.Contains(w.Status));

        if (!string.IsNullOrWhiteSpace(locationId))
        {
            string loc = locationId.Trim();
            query = query.Where(w => w.AssignedLocationId == loc);
        }

        return await query
            .OrderBy(w => w.CreatedAt)
            .Take(100)
            .Select(w => new OperationalProjectionSummary
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
            })
            .ToListAsync(cancellationToken);
    }
}
