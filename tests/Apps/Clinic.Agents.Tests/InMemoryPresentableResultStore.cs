using System.Collections.Concurrent;
using System.Globalization;
using Vitarish.Intelligence.Application.GuidedSurfaces;

namespace Vitarish.Clinic.Agents.Tests;

/// <summary>Per-run handle store with the production numbering, for tests that compose a real runner.</summary>
internal sealed class InMemoryPresentableResultStore : IPresentableResultStore
{
    private readonly ConcurrentDictionary<(string Run, string Handle), PresentableResult> results = new();
    private readonly ConcurrentDictionary<string, int> counters = new(StringComparer.Ordinal);

    public Task<string> SaveAsync(string executionId, PresentableResult result, CancellationToken cancellationToken)
    {
        string handle = PresentableHandles.Create(executionId, counters.AddOrUpdate(executionId, 1, (_, current) => current + 1));
        results[(executionId, handle)] = result;
        return Task.FromResult(handle);
    }

    public Task<PresentableResult?> GetAsync(string executionId, string handle, CancellationToken cancellationToken) =>
        Task.FromResult(results.TryGetValue((executionId, handle), out PresentableResult? result) ? result : null);
}
