namespace Vitarish.Journey.Domain;

/// <summary>
/// Request to resolve the journey for a user.
/// </summary>
public sealed record JourneyRequest(
    string UserId,
    string? RequestedReturnUrl,
    string? CurrentPath,
    string? AppKey,
    string? WorkspaceId,
    bool IsAfterLogin
);
