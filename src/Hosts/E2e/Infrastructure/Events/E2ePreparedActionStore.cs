using System.Collections.Concurrent;
using System.Text.Json;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>In-memory prepared-action store. Like the production store, the version of an action is its stored JSON.</summary>
internal sealed class E2ePreparedActionStore : IPreparedActionStore
{
    private readonly ConcurrentDictionary<Guid, string> actions = new();

    public Task SaveNewAsync(PreparedAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        string payload = Serialize(action);
        if (actions.TryAdd(action.ActionId, payload)) return Task.CompletedTask;
        if (actions.TryGetValue(action.ActionId, out string? existing) && existing == payload) return Task.CompletedTask;
        throw new InvalidOperationException("A prepared action with the same id already exists with different data.");
    }

    public Task<StoredPreparedAction?> GetAsync(Guid actionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!actions.TryGetValue(actionId, out string? version)) return Task.FromResult<StoredPreparedAction?>(null);
        PreparedAction action = JsonSerializer.Deserialize<PreparedAction>(version)
            ?? throw new InvalidOperationException("A stored prepared action could not be read.");
        return Task.FromResult<StoredPreparedAction?>(new StoredPreparedAction(action, version));
    }

    public Task<bool> TryReplaceAsync(StoredPreparedAction current, PreparedAction next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(actions.TryUpdate(current.Action.ActionId, Serialize(next), current.Version));
    }

    private static string Serialize(PreparedAction action) => JsonSerializer.Serialize(action);
}
