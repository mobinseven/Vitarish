using MediatR;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Event raised when a user's access claim is removed.
/// </summary>
public sealed record UserAccessClaimRemovedEvent(string UserId, string ClientId) : INotification;
