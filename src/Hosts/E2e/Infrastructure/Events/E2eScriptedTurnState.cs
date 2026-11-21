using System.Collections.Concurrent;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>
/// The shared, in-memory control state of the scripted agent runner: named gates that hold a scripted
/// provider interval open until a scenario releases them, and per-track execution counters a scenario reads
/// back. Every key is chosen by the scenario, so scenarios stay isolated from each other without a reset.
/// </summary>
internal sealed class E2eScriptedTurnState
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource> gates = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TrackCounts> tracks = new(StringComparer.Ordinal);

    /// <summary>Completes when <see cref="Release"/> opens the gate; a gate released before it is awaited stays open.</summary>
    public Task WaitForReleaseAsync(string gate, CancellationToken cancellationToken) =>
        Gate(gate).Task.WaitAsync(cancellationToken);

    public void Release(string gate) => _ = Gate(gate).TrySetResult();

    public void Started(string track) => Interlocked.Increment(ref Counts(track).StartedCount);

    public void Completed(string track) => Interlocked.Increment(ref Counts(track).CompletedCount);

    public void Canceled(string track) => Interlocked.Increment(ref Counts(track).CanceledCount);

    public IReadOnlyDictionary<string, TrackSnapshot> Snapshot() =>
        tracks.ToDictionary(
            pair => pair.Key,
            pair => new TrackSnapshot(pair.Value.StartedCount, pair.Value.CompletedCount, pair.Value.CanceledCount),
            StringComparer.Ordinal);

    private TaskCompletionSource Gate(string gate) =>
        gates.GetOrAdd(gate, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));

    private TrackCounts Counts(string track) => tracks.GetOrAdd(track, _ => new TrackCounts());

    private sealed class TrackCounts
    {
        public int StartedCount;
        public int CompletedCount;
        public int CanceledCount;
    }
}

/// <summary>How many times scripted runs of one track started, completed and were canceled.</summary>
internal sealed record TrackSnapshot(int Started, int Completed, int Canceled);
