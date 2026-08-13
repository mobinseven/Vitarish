namespace Vitarish.Clinic.Application.ClinicalEvents;

public sealed record ClinicalEventScope(
    Guid PatientId,
    string PracticeId,
    string AuthenticatedClinicianId,
    IReadOnlySet<string> Permissions,
    string AgentRunId,
    string Locale,
    string TimeZone);

public sealed record ClinicalEventProvenance(
    string AgentRunId,
    string ClinicianId,
    string Source,
    DateTimeOffset PreparedAt);

public sealed record PatientProcedureContext(
    Guid ProjectId,
    Guid ProcedureId,
    string ProcedureName,
    int NextVisitNumber,
    Guid? PreviousEventId,
    string StateFingerprint);

public sealed record ClinicalEventContent(
    Guid ProcedureId,
    int VisitNumber,
    string EventType,
    string CompletionStatus,
    DateTimeOffset PerformedAt,
    string? Observations,
    string? Diagnosis,
    bool DiagnosisExplicitlyAssertedByClinician,
    string? PlanOrFollowUp,
    IReadOnlyList<string> AttachmentReferences,
    string? NarrativeNote);

public sealed record PrepareClinicalEventRequest(
    ClinicalEventScope Scope,
    ClinicalEventContent Content);

public enum ClinicalEventProposalStatus
{
    Prepared,
    Confirming,
    Completed
}

public sealed record ClinicalEventProposal(
    Guid ActionId,
    ClinicalEventScope Scope,
    string ActionType,
    ClinicalEventContent Content,
    ClinicalEventProvenance Provenance,
    Guid ProjectId,
    Guid ProcedureId,
    Guid? PreviousEventId,
    string StateFingerprint,
    DateTimeOffset ExpiresAt,
    ClinicalEventProposalStatus Status,
    Guid? CreatedEventId = null);

public enum ClinicalEventProposalResultStatus
{
    Prepared,
    PermissionDenied,
    UnknownProcedure,
    InvalidVisitNumber,
    MissingRequiredInformation,
    GeneratedDiagnosisRejected
}

public sealed record ClinicalEventProposalResult(
    ClinicalEventProposalResultStatus Status,
    ClinicalEventProposal? Proposal = null,
    string? Error = null);

public enum ClinicalEventCommitResultStatus
{
    Completed,
    NotFound,
    ScopeMismatch,
    PermissionDenied,
    Expired,
    AlreadyConfirmed,
    ConfirmationInProgress,
    StaleState,
    ProcedureUnavailable
}

public sealed record ClinicalEventCommitResult(
    ClinicalEventCommitResultStatus Status,
    Guid? EventId = null);
