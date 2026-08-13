using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

public sealed class AppointmentAssistantActionCommitter(IPatientAppointmentAgentCapability capability)
    : IAssistantActionCommitter
{
    public string ActionType => "create_appointment";
    public string WorkflowKey => "appointments";

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        AppointmentCommitResult result = await capability.ConfirmAsync(
            action.OwningProposalId, action.PatientId, action.PracticeId, cancellationToken);
        return result.Status switch
        {
            AppointmentCommitStatus.Committed or AppointmentCommitStatus.AlreadyCommitted =>
                AssistantActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            AppointmentCommitStatus.NotFound => AssistantActionCommitResult.Failed("owning_proposal_not_found", "The appointment proposal was not found."),
            AppointmentCommitStatus.Stale => AssistantActionCommitResult.Failed("stale_owning_proposal", "The appointment proposal is stale."),
            AppointmentCommitStatus.Expired => AssistantActionCommitResult.Failed("owning_proposal_expired", "The appointment proposal expired."),
            AppointmentCommitStatus.Forbidden => AssistantActionCommitResult.Failed("owning_workflow_forbidden", "The appointment cannot be confirmed in this scope."),
            _ => AssistantActionCommitResult.Failed("appointment_commit_failed", result.Message ?? "The appointment could not be created.")
        };
    }
}

public sealed class PrescriptionAssistantActionCommitter(IPrescriptionAgentCapability capability)
    : IAssistantActionCommitter
{
    public string ActionType => "create_prescription";
    public string WorkflowKey => "prescriptions";

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.PracticeId, out Guid clinicId) ||
            !Guid.TryParse(context.AuthenticatedUserId, out Guid doctorId))
            return AssistantActionCommitResult.Failed("invalid_prescription_scope", "The prescription scope is invalid.");
        var scope = new PrescriptionAgentScope(
            context.PracticeId, context.PatientId, context.PatientNationalId,
            doctorId, clinicId, context.AuthenticatedUserId);
        PrescriptionConfirmationResult result = await capability.ConfirmAsync(
            action.OwningProposalId, scope, context.AuthenticatedUserId, cancellationToken);
        return result.Status switch
        {
            PrescriptionConfirmationStatus.Submitted or PrescriptionConfirmationStatus.AlreadyConfirmed =>
                AssistantActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            PrescriptionConfirmationStatus.NotFound => AssistantActionCommitResult.Failed("owning_proposal_not_found", "The prescription review was not found."),
            PrescriptionConfirmationStatus.ExpiredOrChangedTemplate => AssistantActionCommitResult.Failed("stale_owning_proposal", "The prescription review is stale."),
            PrescriptionConfirmationStatus.Forbidden => AssistantActionCommitResult.Failed("owning_workflow_forbidden", "The prescription cannot be submitted in this scope."),
            PrescriptionConfirmationStatus.SubmissionFailed => AssistantActionCommitResult.Failed("prescription_submission_failed", result.Message ?? "Prescription submission failed."),
            _ => AssistantActionCommitResult.Failed("invalid_prescription", result.Message ?? "The prescription draft is invalid.", true)
        };
    }
}

public sealed class ClinicalEventAssistantActionCommitter(ConfirmClinicalEventProposal confirm)
    : IAssistantActionCommitter
{
    public string ActionType => "add_clinical_event";
    public string WorkflowKey => "clinical-events";

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        var scope = new ClinicalEventScope(
            context.PatientId, context.PracticeId, context.AuthenticatedUserId,
            context.Permissions, action.AgentRunId, context.Locale, context.TimeZone);
        ClinicalEventCommitResult result = await confirm.ExecuteAsync(action.OwningProposalId, scope, cancellationToken);
        return result.Status switch
        {
            ClinicalEventCommitResultStatus.Completed or ClinicalEventCommitResultStatus.AlreadyConfirmed =>
                AssistantActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            ClinicalEventCommitResultStatus.NotFound => AssistantActionCommitResult.Failed("owning_proposal_not_found", "The clinical-event proposal was not found."),
            ClinicalEventCommitResultStatus.StaleState => AssistantActionCommitResult.Failed("stale_owning_proposal", "The patient procedure changed."),
            ClinicalEventCommitResultStatus.Expired => AssistantActionCommitResult.Failed("owning_proposal_expired", "The clinical-event proposal expired."),
            ClinicalEventCommitResultStatus.ProcedureUnavailable => AssistantActionCommitResult.Failed("procedure_unavailable", "The patient procedure is unavailable.", true),
            ClinicalEventCommitResultStatus.ConfirmationInProgress => AssistantActionCommitResult.Failed("confirmation_in_progress", "Clinical-event confirmation is already in progress.", true),
            _ => AssistantActionCommitResult.Failed("owning_workflow_forbidden", "The clinical event cannot be created in this scope.")
        };
    }
}
