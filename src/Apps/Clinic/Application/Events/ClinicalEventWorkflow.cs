using System.Text;
using System.Text.Json;

namespace Vitarish.Clinic.Application.Events;

public sealed class GetPatientProcedureContext(IClinicalEventProcedureBoundary boundary)
{
    public Task<PatientProcedureContext?> ExecuteAsync(
        ClinicalEventScope scope,
        Guid procedureId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        return scope.Permissions.Contains("ClinicalEvents.Read")
            ? boundary.GetContextAsync(scope.PatientId, scope.PracticeId, procedureId, cancellationToken)
            : Task.FromResult<PatientProcedureContext?>(null);
    }
}

public sealed class PrepareClinicalEvent(
    IClinicalEventProcedureBoundary boundary,
    IClinicalEventProposalStore store,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private const int MaximumAttachmentCount = 20;
    private const int MaximumProposalBytes = 64 * 1024;

    public async Task<ClinicalEventProposalResult> ExecuteAsync(
        PrepareClinicalEventRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ClinicalEventScope scope = request.Scope;
        ClinicalEventContent content = request.Content;
        if (!scope.Permissions.Contains("ClinicalEvents.Create"))
            return new(ClinicalEventProposalResultStatus.PermissionDenied, Error: "Clinical event creation is not permitted.");
        if (content.ProcedureId == Guid.Empty || string.IsNullOrWhiteSpace(content.EventType) ||
            string.IsNullOrWhiteSpace(content.CompletionStatus) || content.PerformedAt == default ||
            !HasClinicalDetail(content))
            return new(ClinicalEventProposalResultStatus.MissingRequiredInformation,
                Error: "Procedure, event type, completion status, performed time, and clinical detail are required.");
        if (!string.IsNullOrWhiteSpace(content.Diagnosis) && !content.DiagnosisExplicitlyAssertedByClinician)
            return new(ClinicalEventProposalResultStatus.GeneratedDiagnosisRejected,
                Error: "A diagnosis requires an explicit clinician assertion.");
        if (content.AttachmentReferences.Count > MaximumAttachmentCount ||
            Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(content)) > MaximumProposalBytes)
            return new(ClinicalEventProposalResultStatus.MissingRequiredInformation,
                Error: "The clinical event proposal exceeds the allowed size.");

        PatientProcedureContext? procedure = await boundary.GetContextAsync(
            scope.PatientId, scope.PracticeId, content.ProcedureId, cancellationToken);
        if (procedure is null)
            return new(ClinicalEventProposalResultStatus.UnknownProcedure,
                Error: "The procedure does not belong to the current patient.");
        if (content.VisitNumber != procedure.NextVisitNumber)
            return new(ClinicalEventProposalResultStatus.InvalidVisitNumber,
                Error: $"The next valid visit number is {procedure.NextVisitNumber}.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        var proposal = new ClinicalEventProposal(
            Guid.NewGuid(), scope, "add_clinical_event", content,
            new(scope.AgentRunId, scope.AuthenticatedClinicianId, "AgentAssistedClinicianAssertion", now),
            procedure.ProjectId, procedure.ProcedureId, procedure.PreviousEventId,
            procedure.StateFingerprint, now.Add(Lifetime), ClinicalEventProposalStatus.Prepared);
        await store.SaveAsync(proposal, cancellationToken);
        return new(ClinicalEventProposalResultStatus.Prepared, proposal);
    }

    private static bool HasClinicalDetail(ClinicalEventContent content) =>
        !string.IsNullOrWhiteSpace(content.Observations) ||
        !string.IsNullOrWhiteSpace(content.Diagnosis) ||
        !string.IsNullOrWhiteSpace(content.PlanOrFollowUp) ||
        !string.IsNullOrWhiteSpace(content.NarrativeNote) ||
        content.AttachmentReferences.Count > 0;
}

public sealed class ConfirmClinicalEventProposal(
    IClinicalEventProcedureBoundary boundary,
    IClinicalEventProposalStore store,
    TimeProvider timeProvider)
{
    public async Task<ClinicalEventCommitResult> ExecuteAsync(
        Guid actionId,
        ClinicalEventScope scope,
        CancellationToken cancellationToken = default)
    {
        ClinicalEventProposal? proposal = await store.GetAsync(actionId, cancellationToken);
        if (proposal is null) return new(ClinicalEventCommitResultStatus.NotFound);
        if (proposal.Scope.PatientId != scope.PatientId ||
            !string.Equals(proposal.Scope.PracticeId, scope.PracticeId, StringComparison.Ordinal) ||
            !string.Equals(proposal.Scope.AuthenticatedClinicianId, scope.AuthenticatedClinicianId, StringComparison.Ordinal))
            return new(ClinicalEventCommitResultStatus.ScopeMismatch);
        if (!scope.Permissions.Contains("ClinicalEvents.Create"))
            return new(ClinicalEventCommitResultStatus.PermissionDenied);
        if (proposal.Status == ClinicalEventProposalStatus.Completed)
            return new(ClinicalEventCommitResultStatus.AlreadyConfirmed, proposal.CreatedEventId);
        if (proposal.Status == ClinicalEventProposalStatus.Confirming)
            return new(ClinicalEventCommitResultStatus.ConfirmationInProgress);
        if (proposal.ExpiresAt <= timeProvider.GetUtcNow())
            return new(ClinicalEventCommitResultStatus.Expired);

        PatientProcedureContext? current = await boundary.GetContextAsync(
            scope.PatientId, scope.PracticeId, proposal.ProcedureId, cancellationToken);
        if (current is null) return new(ClinicalEventCommitResultStatus.ProcedureUnavailable);
        if (!string.Equals(current.StateFingerprint, proposal.StateFingerprint, StringComparison.Ordinal))
            return new(ClinicalEventCommitResultStatus.StaleState);
        if (!await store.TryBeginConfirmationAsync(actionId, cancellationToken))
        {
            ClinicalEventProposal? latest = await store.GetAsync(actionId, cancellationToken);
            return latest?.Status == ClinicalEventProposalStatus.Completed
                ? new(ClinicalEventCommitResultStatus.AlreadyConfirmed, latest.CreatedEventId)
                : new(ClinicalEventCommitResultStatus.ConfirmationInProgress);
        }

        string payload = JsonSerializer.Serialize(proposal.Content);
        Guid? eventId;
        try
        {
            eventId = await boundary.AddEventAsync(
                current, BuildTitle(proposal.Content), payload, cancellationToken);
        }
        catch
        {
            // The external mutation may have succeeded. Keep the proposal claimed to prevent an unsafe retry.
            throw;
        }

        if (eventId is null)
        {
            await store.TryReleaseConfirmationAsync(actionId, cancellationToken);
            return new(ClinicalEventCommitResultStatus.ProcedureUnavailable);
        }
        if (!await store.CompleteAsync(actionId, eventId.Value, cancellationToken))
            return new(ClinicalEventCommitResultStatus.ConfirmationInProgress);
        return new(ClinicalEventCommitResultStatus.Completed, eventId);
    }

    private static string BuildTitle(ClinicalEventContent content) =>
        $"Visit {content.VisitNumber}: {content.EventType}";
}
