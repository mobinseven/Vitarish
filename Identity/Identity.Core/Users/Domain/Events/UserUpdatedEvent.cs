using MediatR;

namespace Vitarish.Identity.Core.Users.Domain.Events;

/// <summary>
/// Event raised when a user is updated.
/// </summary>
public sealed record UserUpdatedEvent(string UserId) : INotification;
