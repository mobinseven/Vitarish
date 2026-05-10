using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ConflictHandlerService(ClinicalDbContext dbContext) : IConflictHandler
{
    public async Task<bool> IsDuplicateActionAsync(
        Guid workflowId,
        ClinicalActionType actionType,
        TimeSpan? window = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan effectiveWindow = window ?? TimeSpan.FromMinutes(5);
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - effectiveWindow;

        return await dbContext.ClinicalActionRecords
            .AsNoTracking()
            .AnyAsync(r =>
                r.WorkflowId == workflowId &&
                r.ActionType == actionType &&
                r.CommitStatus == "Succeeded" &&
                r.OccurredAt >= cutoff,
                cancellationToken);
    }

    public async Task<bool> IsStaleVersionAsync(
        string resourceType,
        string resourceId,
        string? expectedVersionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedVersionId))
        {
            return false;
        }

        ClinicalResourceLink? link = await dbContext.ResourceLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l =>
                l.ResourceType == resourceType &&
                l.ResourceId == resourceId &&
                !l.IsArchived,
                cancellationToken);

        if (link is null)
        {
            return false;
        }

        return !string.Equals(link.ResourceVersionId, expectedVersionId, StringComparison.Ordinal);
    }

    public bool IsRetriableSafe(ClinicalActionType actionType) => actionType switch
    {
        ClinicalActionType.RecordVitals => false,
        ClinicalActionType.AddDiagnosis => false,
        ClinicalActionType.AddMedicationRequest => false,
        ClinicalActionType.AddClinicalNote => false,
        ClinicalActionType.RegisterPatient => false,
        ClinicalActionType.StartEncounter => false,
        ClinicalActionType.CheckInPatient => true,
        ClinicalActionType.CompleteVisit => true,
        ClinicalActionType.CancelVisit => true,
        ClinicalActionType.ReopenVisit => true,
        ClinicalActionType.SignNote => true,
        _ => false
    };
}
