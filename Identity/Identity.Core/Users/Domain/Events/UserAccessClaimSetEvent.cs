using MediatR;

namespace Vitarish.Identity.Core.Users.Domain.Events;

/// <summary>
/// Raised when an access claim is set for a user.
/// </summary>
public sealed record UserAccessClaimSetEvent(string UserId, string ClientId, string AccessValue) : INotification;
