using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

public sealed class AppointmentClinicWorkspaceActionCommitter(IPatientAppointmentAgentCapability capability)
    : IClinicWorkspaceActionCommitter
{
    public string ActionType => "create_appointment";
    public string WorkflowKey => "appointments";

    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        AppointmentCommitResult result = await capability.ConfirmAsync(
            action.OwningProposalId, action.PatientId, action.PracticeId, cancellationToken);
        return result.Status switch
        {
            AppointmentCommitStatus.Committed or AppointmentCommitStatus.AlreadyCommitted =>
                ClinicWorkspaceActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            AppointmentCommitStatus.NotFound => ClinicWorkspaceActionCommitResult.Failed("owning_proposal_not_found", "The appointment proposal was not found."),
            AppointmentCommitStatus.Stale => ClinicWorkspaceActionCommitResult.Failed("stale_owning_proposal", "The appointment proposal is stale."),
            AppointmentCommitStatus.Expired => ClinicWorkspaceActionCommitResult.Failed("owning_proposal_expired", "The appointment proposal expired."),
            AppointmentCommitStatus.Forbidden => ClinicWorkspaceActionCommitResult.Failed("owning_workflow_forbidden", "The appointment cannot be confirmed in this scope."),
            _ => ClinicWorkspaceActionCommitResult.Failed("appointment_commit_failed", result.Message ?? "The appointment could not be created.")
        };
    }
}

public sealed class PrescriptionClinicWorkspaceActionCommitter(IPrescriptionAgentCapability capability)
    : IClinicWorkspaceActionCommitter
{
    public string ActionType => "create_prescription";
    public string WorkflowKey => "prescriptions";

    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.PracticeId, out Guid clinicId) ||
            !Guid.TryParse(context.AuthenticatedUserId, out Guid doctorId))
            return ClinicWorkspaceActionCommitResult.Failed("invalid_prescription_scope", "The prescription scope is invalid.");
        var scope = new PrescriptionAgentScope(
            context.PracticeId, context.PatientId, context.PatientNationalId,
            doctorId, clinicId, context.AuthenticatedUserId);
        PrescriptionConfirmationResult result = await capability.ConfirmAsync(
            action.OwningProposalId, scope, context.AuthenticatedUserId, cancellationToken);
        return result.Status switch
        {
            PrescriptionConfirmationStatus.Submitted or PrescriptionConfirmationStatus.AlreadyConfirmed =>
                ClinicWorkspaceActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            PrescriptionConfirmationStatus.NotFound => ClinicWorkspaceActionCommitResult.Failed("owning_proposal_not_found", "The prescription review was not found."),
            PrescriptionConfirmationStatus.ExpiredOrChangedTemplate => ClinicWorkspaceActionCommitResult.Failed("stale_owning_proposal", "The prescription review is stale."),
            PrescriptionConfirmationStatus.Forbidden => ClinicWorkspaceActionCommitResult.Failed("owning_workflow_forbidden", "The prescription cannot be submitted in this scope."),
            PrescriptionConfirmationStatus.SubmissionFailed => ClinicWorkspaceActionCommitResult.Failed("prescription_submission_failed", result.Message ?? "Prescription submission failed."),
            _ => ClinicWorkspaceActionCommitResult.Failed("invalid_prescription", result.Message ?? "The prescription draft is invalid.", true)
        };
    }
}

public sealed class ClinicalEventClinicWorkspaceActionCommitter(ConfirmClinicalEventProposal confirm)
    : IClinicWorkspaceActionCommitter
{
    public string ActionType => "add_clinical_event";
    public string WorkflowKey => "clinical-events";

    public async Task<ClinicWorkspaceActionCommitResult> CommitAsync(ClinicWorkspaceAction action, ClinicWorkspaceContext context, CancellationToken cancellationToken)
    {
        var scope = new ClinicalEventScope(
            context.PatientId, context.PracticeId, context.AuthenticatedUserId,
            context.Permissions, action.AgentRunId, context.Locale, context.TimeZone);
        ClinicalEventCommitResult result = await confirm.ExecuteAsync(action.OwningProposalId, scope, cancellationToken);
        return result.Status switch
        {
            ClinicalEventCommitResultStatus.Completed or ClinicalEventCommitResultStatus.AlreadyConfirmed =>
                ClinicWorkspaceActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            ClinicalEventCommitResultStatus.NotFound => ClinicWorkspaceActionCommitResult.Failed("owning_proposal_not_found", "The clinical-event proposal was not found."),
            ClinicalEventCommitResultStatus.StaleState => ClinicWorkspaceActionCommitResult.Failed("stale_owning_proposal", "The patient procedure changed."),
            ClinicalEventCommitResultStatus.Expired => ClinicWorkspaceActionCommitResult.Failed("owning_proposal_expired", "The clinical-event proposal expired."),
            ClinicalEventCommitResultStatus.ProcedureUnavailable => ClinicWorkspaceActionCommitResult.Failed("procedure_unavailable", "The patient procedure is unavailable.", true),
            ClinicalEventCommitResultStatus.ConfirmationInProgress => ClinicWorkspaceActionCommitResult.Failed("confirmation_in_progress", "Clinical-event confirmation is already in progress.", true),
            _ => ClinicWorkspaceActionCommitResult.Failed("owning_workflow_forbidden", "The clinical event cannot be created in this scope.")
        };
    }
}
