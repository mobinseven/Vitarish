using Vitarish.Base.Domain.Primitives;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Raised when an access claim is set for a user.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="ClientId">The identifier of the client application.</param>
/// <param name="AccessValue">The value of the access claim.</param>
public sealed record UserAccessClaimSetEvent(
    string UserId,
    string ClientId,
    string AccessValue) : DomainEventBase;
