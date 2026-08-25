using System.Collections.Concurrent;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;

namespace Vitarish.E2e.Infrastructure.Events;

internal sealed class E2eAssistantActionStore : IAssistantActionStore
{
    private readonly ConcurrentDictionary<Guid, AssistantAction> actions = new();

    public Task SaveAsync(AssistantAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (actions.TryAdd(action.ActionId, action)) return Task.CompletedTask;
        if (actions.TryGetValue(action.ActionId, out AssistantAction? existing) && existing == action) return Task.CompletedTask;
        throw new InvalidOperationException("A clinic-assistant action with the same id already exists with different data.");
    }

    public Task<AssistantAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(actions.TryGetValue(actionId, out AssistantAction? action) ? action : null);
    }

    public Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, current => current.Status == AssistantActionStatus.Proposed || retryFailed && current.Status == AssistantActionStatus.Failed && current.Failure?.Retryable == true,
            current => current with { Status = AssistantActionStatus.Confirming, Failure = null }, cancellationToken);

    public Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, current => current.Status == AssistantActionStatus.Proposed,
            current => current with { Status = AssistantActionStatus.Rejected, RejectedAt = rejectedAt, RejectionReason = reason }, cancellationToken);

    public Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, current => current.Status == AssistantActionStatus.Confirming,
            current => current with { Status = AssistantActionStatus.Completed, ConfirmedAt = confirmedAt, ConfirmedByUserId = confirmedByUserId, CompletionPayload = completionPayload }, cancellationToken);

    public Task<bool> FailAsync(Guid actionId, AssistantActionFailure failure, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, current => current.Status is AssistantActionStatus.Proposed or AssistantActionStatus.Confirming,
            current => current with { Status = AssistantActionStatus.Failed, Failure = failure }, cancellationToken);

    public Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken) =>
        TransitionAsync(actionId, current => current.Status is AssistantActionStatus.Proposed or AssistantActionStatus.Failed,
            current => current with { Status = AssistantActionStatus.Expired }, cancellationToken);

    private Task<bool> TransitionAsync(Guid actionId, Func<AssistantAction, bool> allowed, Func<AssistantAction, AssistantAction> update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        while (actions.TryGetValue(actionId, out AssistantAction? current))
        {
            if (!allowed(current)) return Task.FromResult(false);
            if (actions.TryUpdate(actionId, update(current), current)) return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}
