using MediatR;

namespace Vorn.Aaas.Parties.Domain.Events;

/// <summary>
/// Raised when a new tenant is created.
/// </summary>
public sealed record TenantCreatedEvent(Guid TenantId, Guid OwnerId) : INotification;
