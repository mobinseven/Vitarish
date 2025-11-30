namespace Vitarish.Identity.Contracts.Events;

/// <summary>
/// Integration event raised when a new authentication client is created.
/// Used for cross-service communication.
/// </summary>
/// <param name="ClientId">The identifier of the newly created client.</param>
/// <param name="OccurredAt">When the event occurred.</param>
public sealed record AuthenticationClientCreatedIntegrationEvent(
    string ClientId,
    DateTimeOffset OccurredAt);
