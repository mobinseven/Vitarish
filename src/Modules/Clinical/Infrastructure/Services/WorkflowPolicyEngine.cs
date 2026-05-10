using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class WorkflowPolicyEngine : IWorkflowPolicyEngine
{
    public Task<IReadOnlyList<WorkflowValidationResult>> EvaluateAsync(
        string actorId,
        IReadOnlyList<string> actorRoles,
        ClinicalActionType actionType,
        ClinicalWorkflow? workflow,
        WorkflowPolicyDefinition policy,
        object? payload = null,
        CancellationToken cancellationToken = default)
    {
        List<WorkflowValidationResult> errors = [];

        // Role check
        if (policy.AllowedRolesByActionType.TryGetValue(actionType, out string[]? allowedRoles) && allowedRoles.Length > 0)
        {
            bool hasRole = actorRoles.Any(role =>
                allowedRoles.Any(allowed => string.Equals(allowed, role, StringComparison.OrdinalIgnoreCase)));

            if (!hasRole)
            {
                errors.Add(WorkflowValidationResult.Error(
                    "ROLE_DENIED",
                    $"Action '{actionType}' requires one of the following roles: {string.Join(", ", allowedRoles)}. Actor has: {string.Join(", ", actorRoles)}.",
                    "ActorRole"));
            }
        }

        // Lifecycle / workflow status check
        if (workflow is not null)
        {
            VisitWorkflowStatus currentStatus = workflow.Status;

            // For transition actions, check allowed transitions
            VisitWorkflowStatus? targetStatus = DetermineTargetStatus(actionType);
            if (targetStatus.HasValue &&
                policy.AllowedTransitionsByStatus.TryGetValue(currentStatus, out VisitWorkflowStatus[]? allowedTargets))
            {
                if (!allowedTargets.Contains(targetStatus.Value))
                {
                    errors.Add(WorkflowValidationResult.Error(
                        "INVALID_TRANSITION",
                        $"Cannot transition from '{currentStatus}' to '{targetStatus.Value}' for action '{actionType}'.",
                        "WorkflowStatus"));
                }
            }

            // For clinical documentation actions, check that workflow is in an active state
            if (IsDocumentationAction(actionType) &&
                workflow.Status is VisitWorkflowStatus.Completed or VisitWorkflowStatus.Cancelled)
            {
                errors.Add(WorkflowValidationResult.Error(
                    "WORKFLOW_CLOSED",
                    $"Cannot perform '{actionType}' on a workflow with status '{currentStatus}'.",
                    "WorkflowStatus"));
            }
        }

        // Required steps check before completion
        if (actionType == ClinicalActionType.CompleteVisit && workflow is not null &&
            policy.RequiredStepsByStatus.TryGetValue(VisitWorkflowStatus.Completed, out ClinicalActionType[]? requiredSteps))
        {
            // Required steps validation is advisory — actual completed steps come from ClinicalActionRecords
            // This engine validates structurally; completion record verification happens in ClinicalActionService
        }

        // Notes require signature check
        if (actionType == ClinicalActionType.CompleteVisit && policy.NotesRequireSignature)
        {
            // Advisory warning — ClinicalActionService will check actual unsigned notes
            errors.Add(WorkflowValidationResult.Warning(
                "NOTES_REQUIRE_SIGNATURE",
                "Ensure all clinical notes are signed before completing the visit.",
                "Notes"));
        }

        // Reopen check
        if (actionType == ClinicalActionType.ReopenVisit && !policy.AllowReopenAfterCancel &&
            workflow?.Status == VisitWorkflowStatus.Cancelled)
        {
            errors.Add(WorkflowValidationResult.Error(
                "REOPEN_NOT_ALLOWED",
                "Reopening a cancelled visit is not permitted by the current workflow policy.",
                "WorkflowStatus"));
        }

        return Task.FromResult<IReadOnlyList<WorkflowValidationResult>>(errors);
    }

    private static VisitWorkflowStatus? DetermineTargetStatus(ClinicalActionType actionType) => actionType switch
    {
        ClinicalActionType.CheckInPatient => VisitWorkflowStatus.CheckedIn,
        ClinicalActionType.StartEncounter => VisitWorkflowStatus.InProgress,
        ClinicalActionType.CompleteVisit => VisitWorkflowStatus.Completed,
        ClinicalActionType.CancelVisit => VisitWorkflowStatus.Cancelled,
        ClinicalActionType.ReopenVisit => VisitWorkflowStatus.Draft,
        ClinicalActionType.SignNote => VisitWorkflowStatus.Signed,
        _ => null
    };

    private static bool IsDocumentationAction(ClinicalActionType actionType) => actionType is
        ClinicalActionType.RecordVitals or
        ClinicalActionType.AddDiagnosis or
        ClinicalActionType.AddMedicationRequest or
        ClinicalActionType.AddClinicalNote or
        ClinicalActionType.SignNote;
}
