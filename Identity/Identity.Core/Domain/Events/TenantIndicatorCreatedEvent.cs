using MediatR;

namespace Vitarish.Identity.Core.Domain.Events;

/// <summary>
/// Event raised when a tenant indicator is created.
/// </summary>
public sealed record TenantIndicatorCreatedEvent(Guid TenantId, Guid IndicatorId) : INotification;
