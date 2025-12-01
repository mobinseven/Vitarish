using MediatR;

namespace Vitarish.Identity.Core.Users.Domain.Events;

/// <summary>
/// Raised when an access claim is removed from a user.
/// </summary>
public sealed record UserAccessClaimRemovedEvent(string UserId, string ClientId) : INotification;
