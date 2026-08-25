using System.Collections.Concurrent;
using Vitarish.Clinic.Application.Events;

namespace Vitarish.E2e.Infrastructure.Events;

internal sealed class E2eClinicalEventProposalStore : IClinicalEventProposalStore
{
    private readonly ConcurrentDictionary<Guid, ClinicalEventProposal> proposals = new();

    public Task SaveAsync(ClinicalEventProposal proposal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        cancellationToken.ThrowIfCancellationRequested();
        if (!proposals.TryAdd(proposal.ActionId, proposal with { Status = ClinicalEventProposalStatus.Prepared }))
            throw new InvalidOperationException("A clinical event proposal with the same action id already exists.");
        return Task.CompletedTask;
    }

    public Task<ClinicalEventProposal?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!proposals.TryGetValue(actionId, out ClinicalEventProposal? proposal))
            return Task.FromResult<ClinicalEventProposal?>(null);
        if (proposal.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _ = proposals.TryRemove(actionId, out _);
            return Task.FromResult<ClinicalEventProposal?>(null);
        }
        return Task.FromResult<ClinicalEventProposal?>(proposal);
    }

    public Task<bool> TryBeginConfirmationAsync(Guid actionId, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, ClinicalEventProposalStatus.Prepared, ClinicalEventProposalStatus.Confirming, cancellationToken);

    public Task<bool> TryReleaseConfirmationAsync(Guid actionId, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, ClinicalEventProposalStatus.Confirming, ClinicalEventProposalStatus.Prepared, cancellationToken);

    public Task<bool> CompleteAsync(Guid actionId, Guid eventId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (proposals.TryGetValue(actionId, out ClinicalEventProposal? current))
        {
            if (current.Status != ClinicalEventProposalStatus.Confirming)
                return Task.FromResult(false);
            ClinicalEventProposal completed = current with { Status = ClinicalEventProposalStatus.Completed, CreatedEventId = eventId };
            if (proposals.TryUpdate(actionId, completed, current))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    private Task<bool> TransitionAsync(Guid actionId, ClinicalEventProposalStatus expected, ClinicalEventProposalStatus next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (proposals.TryGetValue(actionId, out ClinicalEventProposal? current))
        {
            if (current.Status != expected)
                return Task.FromResult(false);
            if (proposals.TryUpdate(actionId, current with { Status = next }, current))
                return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
