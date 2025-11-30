namespace Vitarish.Identity.Contracts.Events;

/// <summary>
/// Integration event raised when a user's access claim is removed.
/// Used for cross-service communication.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="ClientId">The client identifier.</param>
/// <param name="OccurredAt">When the event occurred.</param>
public sealed record UserAccessClaimRemovedIntegrationEvent(
    string UserId,
    string ClientId,
    DateTimeOffset OccurredAt);
