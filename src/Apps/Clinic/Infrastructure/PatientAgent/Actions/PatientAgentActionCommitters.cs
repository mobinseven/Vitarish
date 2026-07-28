using System.Text.Json;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Interface.AgentCapabilities;
using Vitarish.Clinic.Application.ClinicalEvents;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;
using Vitarish.Medical.Interface.AgentCapabilities.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.PatientAgent.Actions;

public sealed class AppointmentPatientAgentActionCommitter(IPatientAppointmentAgentCapability capability)
    : IPatientAgentActionCommitter
{
    public string ActionType => "create_appointment";

    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
    {
        AppointmentCommitResult result = await capability.ConfirmAsync(
            action.OwningProposalId, action.PatientId, action.PracticeId, cancellationToken);
        return result.Status switch
        {
            AppointmentCommitStatus.Committed or AppointmentCommitStatus.AlreadyCommitted =>
                PatientAgentActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            AppointmentCommitStatus.NotFound => PatientAgentActionCommitResult.Failed("owning_proposal_not_found", "The appointment proposal was not found."),
            AppointmentCommitStatus.Stale => PatientAgentActionCommitResult.Failed("stale_owning_proposal", "The appointment proposal is stale.", true),
            AppointmentCommitStatus.Expired => PatientAgentActionCommitResult.Failed("owning_proposal_expired", "The appointment proposal expired."),
            AppointmentCommitStatus.Forbidden => PatientAgentActionCommitResult.Failed("owning_workflow_forbidden", "The appointment cannot be confirmed in this scope."),
            _ => PatientAgentActionCommitResult.Failed("appointment_commit_failed", result.Message ?? "The appointment could not be created.", true)
        };
    }
}

public sealed class PrescriptionPatientAgentActionCommitter(IPrescriptionAgentCapability capability)
    : IPatientAgentActionCommitter
{
    public string ActionType => "create_prescription";

    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(context.PracticeId, out Guid clinicId) ||
            !Guid.TryParse(context.AuthenticatedUserId, out Guid doctorId))
            return PatientAgentActionCommitResult.Failed("invalid_prescription_scope", "The prescription scope is invalid.");
        var scope = new PrescriptionAgentScope(
            context.PracticeId, context.PatientId, context.PatientNationalId,
            doctorId, clinicId, context.AuthenticatedUserId);
        PrescriptionConfirmationResult result = await capability.ConfirmAsync(
            action.OwningProposalId, scope, context.AuthenticatedUserId, cancellationToken);
        return result.Status switch
        {
            PrescriptionConfirmationStatus.Submitted or PrescriptionConfirmationStatus.AlreadyConfirmed =>
                PatientAgentActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            PrescriptionConfirmationStatus.NotFound => PatientAgentActionCommitResult.Failed("owning_proposal_not_found", "The prescription review was not found."),
            PrescriptionConfirmationStatus.ExpiredOrChangedTemplate => PatientAgentActionCommitResult.Failed("stale_owning_proposal", "The prescription review is stale."),
            PrescriptionConfirmationStatus.Forbidden => PatientAgentActionCommitResult.Failed("owning_workflow_forbidden", "The prescription cannot be submitted in this scope."),
            PrescriptionConfirmationStatus.SubmissionFailed => PatientAgentActionCommitResult.Failed("prescription_submission_failed", result.Message ?? "Prescription submission failed."),
            _ => PatientAgentActionCommitResult.Failed("invalid_prescription", result.Message ?? "The prescription draft is invalid.", true)
        };
    }
}

public sealed class ClinicalEventPatientAgentActionCommitter(ConfirmClinicalEventProposal confirm)
    : IPatientAgentActionCommitter
{
    public string ActionType => "add_clinical_event";

    public async Task<PatientAgentActionCommitResult> CommitAsync(PatientAgentAction action, PatientAgentContext context, CancellationToken cancellationToken)
    {
        var scope = new ClinicalEventScope(
            context.PatientId, context.PracticeId, context.AuthenticatedUserId,
            context.Permissions, action.AgentRunId, context.Locale, context.TimeZone);
        ClinicalEventCommitResult result = await confirm.ExecuteAsync(action.OwningProposalId, scope, cancellationToken);
        return result.Status switch
        {
            ClinicalEventCommitResultStatus.Completed or ClinicalEventCommitResultStatus.AlreadyConfirmed =>
                PatientAgentActionCommitResult.Completed(JsonSerializer.Serialize(result)),
            ClinicalEventCommitResultStatus.NotFound => PatientAgentActionCommitResult.Failed("owning_proposal_not_found", "The clinical-event proposal was not found."),
            ClinicalEventCommitResultStatus.StaleState => PatientAgentActionCommitResult.Failed("stale_owning_proposal", "The patient procedure changed.", true),
            ClinicalEventCommitResultStatus.Expired => PatientAgentActionCommitResult.Failed("owning_proposal_expired", "The clinical-event proposal expired."),
            ClinicalEventCommitResultStatus.ProcedureUnavailable => PatientAgentActionCommitResult.Failed("procedure_unavailable", "The patient procedure is unavailable.", true),
            ClinicalEventCommitResultStatus.ConfirmationInProgress => PatientAgentActionCommitResult.Failed("confirmation_in_progress", "Clinical-event confirmation is already in progress.", true),
            _ => PatientAgentActionCommitResult.Failed("owning_workflow_forbidden", "The clinical event cannot be created in this scope.")
        };
    }
}
