using System.Text.Json;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Clinical.Infrastructure.Mapping;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class StatusCoordinatorService(
    ClinicalDbContext dbContext,
    IFhirGateway fhirGateway,
    IVisitWorkflowService workflowService) : IStatusCoordinator
{
    public async Task<VisitWorkflowStatus?> ReconcileAsync(
        ClinicalWorkflow workflow,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workflow.EncounterFhirReference))
        {
            return null;
        }

        string[] parts = workflow.EncounterFhirReference.Split('/');
        if (parts.Length < 2)
        {
            return null;
        }

        string resourceType = parts[^2];
        string resourceId = parts[^1];

        try
        {
            FhirOperationResponse response = await fhirGateway.ExecuteAsync(new FhirOperationRequest
            {
                ResourceType = resourceType,
                ResourceId = resourceId,
                Interaction = FhirInteractionType.Read
            }, cancellationToken: cancellationToken);

            if (response.Status != FhirOperationStatus.Succeeded || string.IsNullOrWhiteSpace(response.ResponseJson))
            {
                return null;
            }

            string? encounterStatus = ExtractEncounterStatus(response.ResponseJson);
            VisitWorkflowStatus? reconciled = MapEncounterStatus(encounterStatus, workflow.Status);

            if (reconciled.HasValue && reconciled.Value != workflow.Status)
            {
                await workflowService.TransitionAsync(
                    workflow.Id,
                    reconciled.Value,
                    "system:status-coordinator",
                    $"Reconciled from FHIR Encounter.status='{encounterStatus}'",
                    cancellationToken);

                return reconciled.Value;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractEncounterStatus(string responseJson)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("status", out JsonElement statusElement))
            {
                return statusElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static VisitWorkflowStatus? MapEncounterStatus(string? encounterStatus, VisitWorkflowStatus currentStatus) =>
        encounterStatus?.ToLowerInvariant() switch
        {
            "finished" when currentStatus is not VisitWorkflowStatus.Completed => VisitWorkflowStatus.Completed,
            "cancelled" when currentStatus is not VisitWorkflowStatus.Cancelled => VisitWorkflowStatus.Cancelled,
            _ => null
        };
}
