using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Application.Assistant.Surgeries;

/// <summary>
/// What a confirmed surgery-workflow action writes, kept on the server between preparation and confirmation.
/// The assistant and the review card only see the review built from it, so a resolved patient never leaves the server.
/// Exactly the part its <see cref="ActionType"/> needs is set.
/// </summary>
/// <param name="SubjectPatientId">The route-bound patient of a patient-scope action; empty for the practice scope.</param>
/// <param name="Surgery">The plan a create or edit writes; an edit carries the surgery id and its read concurrency token.</param>
/// <param name="SurgeryId">The surgery a completion or cancellation changes.</param>
/// <param name="SurgeryTypeId">The surgery type an edit or deletion changes.</param>
/// <param name="ExpectedConcurrencyToken">The token read at preparation; a change made since then makes the commit stale.</param>
public sealed record SurgeryAssistantProposal(
    Guid Id,
    string ActionType,
    string PracticeId,
    string RequestedByUserId,
    string SubjectKind,
    Guid SubjectPatientId,
    DateTimeOffset ExpiresAt,
    SurgeryWriteModel? Surgery = null,
    Guid? SurgeryId = null,
    Guid? SurgeryTypeId = null,
    Guid ExpectedConcurrencyToken = default,
    SurgeryCatalogWriteModel? SurgeryType = null,
    SurgerySettingsWriteModel? CancellationMessage = null);

public interface ISurgeryAssistantProposalStore
{
    /// <summary>Keeps the proposal until shortly after it expires.</summary>
    Task SaveAsync(SurgeryAssistantProposal proposal, CancellationToken cancellationToken);

    Task<SurgeryAssistantProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken);
}
