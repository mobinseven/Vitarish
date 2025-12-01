using MediatR;

namespace Vitarish.Identity.Core.Parties.Domain.Events;

/// <summary>
/// Raised when a new owner is created.
/// </summary>
public sealed record OwnerCreatedEvent(Guid OwnerId) : INotification;
