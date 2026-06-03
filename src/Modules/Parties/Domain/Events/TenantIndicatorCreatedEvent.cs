
using MediatR;

namespace Vitarish.Parties.Domain.Events;
/// <summary>
/// Event raised when a tenant indicator is created.
/// </summary>
public sealed record TenantIndicatorCreatedEvent(Guid TenantId, Guid IndicatorId) : INotification;
