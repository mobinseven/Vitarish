using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Raised when a new authentication client is created.
/// </summary>
/// <param name="ClientId">The identifier of the newly created client.</param>
public sealed record AuthenticationClientCreatedEvent(string ClientId) : DomainEventBase;
