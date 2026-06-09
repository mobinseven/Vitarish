using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformConnectionMemoryQuery
{
    Task<IReadOnlyList<ConnectionMemorySnapshot>> GetRecentSamplesAsync(
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConnectionMemorySnapshot>> GetCircuitSamplesAsync(
        string circuitId,
        CancellationToken cancellationToken);
}
