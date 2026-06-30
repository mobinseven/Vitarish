using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Infrastructure.Workspace;

/// <summary>
/// Reads workspace/app context for journey evaluation.
/// Currently returns a default snapshot since workspace management is out of scope.
/// </summary>
public sealed class JourneyWorkspaceReader : IJourneyWorkspaceReader
{
    public Task<JourneyWorkspaceSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual workspace lookup when workspace management is available
        // For now, return a snapshot that doesn't block the user
        return Task.FromResult<JourneyWorkspaceSnapshot?>(new JourneyWorkspaceSnapshot(
            CurrentWorkspaceId: null,
            CurrentAppKey: null,
            LastUsedWorkspaceId: null,
            LastUsedAppKey: null,
            RequiresWorkspaceSelection: false
        ));
    }
}
