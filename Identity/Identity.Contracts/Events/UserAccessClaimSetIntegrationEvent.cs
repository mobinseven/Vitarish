namespace Vitarish.Identity.Contracts.Events;

/// <summary>
/// Integration event raised when a user's access claim is set.
/// Used for cross-service communication.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="ClientId">The client identifier.</param>
/// <param name="AccessValue">The access claim value.</param>
/// <param name="OccurredAt">When the event occurred.</param>
public sealed record UserAccessClaimSetIntegrationEvent(
    string UserId,
    string ClientId,
    string AccessValue,
    DateTimeOffset OccurredAt);
