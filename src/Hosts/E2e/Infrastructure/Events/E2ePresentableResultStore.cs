using System.Collections.Concurrent;
using System.Globalization;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>In-memory presentable-result store with the production store's per-run handle numbering.</summary>
internal sealed class E2ePresentableResultStore : IPresentableResultStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PresentableResult>> runs = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executionId);
        ArgumentNullException.ThrowIfNull(result);
        cancellationToken.ThrowIfCancellationRequested();
        ConcurrentDictionary<string, PresentableResult> run = runs.GetOrAdd(executionId, _ => new(StringComparer.Ordinal));
        lock (run)
        {
            string handle = "r" + (run.Count + 1).ToString(CultureInfo.InvariantCulture);
            run[handle] = result;
            return Task.FromResult(handle);
        }
    }

    public Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(runs.TryGetValue(executionId, out ConcurrentDictionary<string, PresentableResult>? run) && run.TryGetValue(handle, out PresentableResult? result) ? result : null);
    }
}
