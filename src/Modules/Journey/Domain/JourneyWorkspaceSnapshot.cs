namespace Vitarish.Journey.Domain;

/// <summary>
/// Snapshot of the user's workspace/app context for journey evaluation.
/// </summary>
public sealed record JourneyWorkspaceSnapshot(
    string? CurrentWorkspaceId,
    string? CurrentAppKey,
    string? LastUsedWorkspaceId,
    string? LastUsedAppKey,
    bool RequiresWorkspaceSelection
);
