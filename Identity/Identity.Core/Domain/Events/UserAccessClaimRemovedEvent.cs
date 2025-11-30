using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Raised when an access claim is removed from a user.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="ClientId">The identifier of the client application.</param>
public sealed record UserAccessClaimRemovedEvent(
    string UserId,
    string ClientId) : DomainEventBase;
