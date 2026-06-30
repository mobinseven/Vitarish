namespace Vitarish.Journey.Application.Models;

public sealed record JourneyRequest(
    string UserId,
    string? RequestedReturnUrl,
    string? CurrentPath,
    string? AppKey,
    string? WorkspaceId,
    bool IsAfterLogin);
