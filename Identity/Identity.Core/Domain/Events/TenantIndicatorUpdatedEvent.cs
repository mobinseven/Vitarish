using MediatR;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Event raised when a tenant indicator is updated.
/// </summary>
public sealed record TenantIndicatorUpdatedEvent(Guid TenantId, Guid IndicatorId) : INotification;
