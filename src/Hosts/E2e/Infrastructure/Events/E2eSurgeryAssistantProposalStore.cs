using System.Collections.Concurrent;
using Vitarish.Clinic.Application.Assistant.Surgeries;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>Keeps surgery assistant proposals in memory for the host's lifetime, in place of the Redis store.</summary>
internal sealed class E2eSurgeryAssistantProposalStore : ISurgeryAssistantProposalStore
{
    private readonly ConcurrentDictionary<Guid, SurgeryAssistantProposal> proposals = new();

    public Task SaveAsync(SurgeryAssistantProposal proposal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();
        if (!proposals.TryAdd(proposal.Id, proposal))
            throw new InvalidOperationException("A surgery proposal with the same id already exists.");
        return Task.CompletedTask;
    }

    public Task<SurgeryAssistantProposal?> GetAsync(Guid proposalId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(proposals.TryGetValue(proposalId, out SurgeryAssistantProposal? proposal) ? proposal : null);
    }
}
