
using MediatR;

namespace Vorn.Aaas.Parties.Domain.Events;
/// <summary>
/// Event raised when a tenant indicator is updated.
/// </summary>
public sealed record TenantIndicatorUpdatedEvent(Guid TenantId, Guid IndicatorId) : INotification;
