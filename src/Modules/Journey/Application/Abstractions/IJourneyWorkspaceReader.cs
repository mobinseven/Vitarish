using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Reads workspace/app context for journey evaluation.
/// </summary>
public interface IJourneyWorkspaceReader
{
    /// <summary>
    /// Gets a snapshot of the user's workspace/app context.
    /// </summary>
    Task<JourneyWorkspaceSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default);
}
