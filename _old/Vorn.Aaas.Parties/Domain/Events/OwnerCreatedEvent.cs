using MediatR;

namespace Vorn.Aaas.Parties.Domain.Events;

/// <summary>
/// Raised when a new owner is created.
/// </summary>
public sealed record OwnerCreatedEvent(Guid OwnerId) : INotification;
