using MediatR;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Raised when a new owner is created.
/// </summary>
public sealed record OwnerCreatedEvent(Guid OwnerId) : INotification;
