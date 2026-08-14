namespace Vitarish.Clinic.Application.Events;

public interface IClinicalEventProcedureBoundary
{
    Task<PatientProcedureContext?> GetContextAsync(
        Guid patientId,
        string practiceId,
        Guid procedureId,
        CancellationToken cancellationToken);

    Task<Guid?> AddEventAsync(
        PatientProcedureContext context,
        string title,
        string opaquePayload,
        CancellationToken cancellationToken);
}

public interface IClinicalEventProposalStore
{
    Task SaveAsync(ClinicalEventProposal proposal, CancellationToken cancellationToken);
    Task<ClinicalEventProposal?> GetAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryBeginConfirmationAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryReleaseConfirmationAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid actionId, Guid eventId, CancellationToken cancellationToken);
}
