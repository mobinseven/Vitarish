using MediatR;

namespace Vitarish.Identity.Core.Users.Domain.Events;

/// <summary>
/// Raised when a new user is created.
/// </summary>
public sealed record UserCreatedEvent(string UserId) : INotification;
