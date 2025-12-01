using MediatR;

namespace Vitarish.Identity.Core.Users.Domain.Events;

/// <summary>
/// Event raised when a user is deleted.
/// </summary>
public sealed record UserDeletedEvent(string UserId) : INotification;
