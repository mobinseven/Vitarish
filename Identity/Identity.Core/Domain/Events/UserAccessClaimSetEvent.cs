using MediatR;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Event raised when a user's access claim is set or updated.
/// </summary>
public sealed record UserAccessClaimSetEvent(string UserId, string ClientId, string AccessValue) : INotification;
