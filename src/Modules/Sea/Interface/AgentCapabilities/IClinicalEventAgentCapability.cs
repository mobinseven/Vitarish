using System.Collections.Concurrent;
using Vitarish.Sea.Application.AgentCapabilities.ClinicalEvents;

namespace Vitarish.Sea.Interface.AgentCapabilities;

public interface IClinicalEventAgentCapability
{
    Task<PatientProcedureContext?> GetPatientProcedureContextAsync(ClinicalEventScope scope, Guid procedureId, CancellationToken cancellationToken);
    Task<ClinicalEventProposalResult> PrepareClinicalEventAsync(PrepareClinicalEventRequest request, CancellationToken cancellationToken);
    Task<ClinicalEventCommitResult> ConfirmClinicalEventProposalAsync(Guid proposalId, ClinicalEventScope scope, string confirmedByUserId, CancellationToken cancellationToken);
}

public sealed class ClinicalEventAgentCapability(
    GetPatientProcedureContext getContext,
    PrepareClinicalEvent prepare,
    ConfirmClinicalEventProposal confirm) : IClinicalEventAgentCapability
{
    public Task<PatientProcedureContext?> GetPatientProcedureContextAsync(ClinicalEventScope scope, Guid procedureId, CancellationToken cancellationToken) =>
        getContext.ExecuteAsync(scope, procedureId, cancellationToken);
    public Task<ClinicalEventProposalResult> PrepareClinicalEventAsync(PrepareClinicalEventRequest request, CancellationToken cancellationToken) =>
        prepare.ExecuteAsync(request, cancellationToken);
    public Task<ClinicalEventCommitResult> ConfirmClinicalEventProposalAsync(Guid proposalId, ClinicalEventScope scope, string confirmedByUserId, CancellationToken cancellationToken) =>
        confirm.ExecuteAsync(proposalId, scope, confirmedByUserId, cancellationToken);
}

public sealed class InMemoryClinicalEventProposalStore : IClinicalEventProposalStore
{
    private readonly ConcurrentDictionary<Guid, ClinicalEventProposal> _proposals = new();
    private readonly ConcurrentDictionary<Guid, ClinicalEventCommitResult> _commits = new();
    private readonly ConcurrentDictionary<Guid, byte> _confirmations = new();

    public Task<ClinicalEventProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken) => Task.FromResult(_proposals.GetValueOrDefault(proposalId));
    public Task SaveAsync(ClinicalEventProposal proposal, CancellationToken cancellationToken) { _proposals[proposal.Id] = proposal; return Task.CompletedTask; }
    public Task<ClinicalEventCommitResult?> GetCommitAsync(Guid proposalId, CancellationToken cancellationToken) => Task.FromResult(_commits.GetValueOrDefault(proposalId));
    public Task<bool> TryBeginConfirmationAsync(Guid proposalId, CancellationToken cancellationToken) => Task.FromResult(_confirmations.TryAdd(proposalId, 0));
    public Task SaveCommitAsync(Guid proposalId, ClinicalEventCommitResult result, CancellationToken cancellationToken) { _commits[proposalId] = result; return Task.CompletedTask; }
}
