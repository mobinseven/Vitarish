using MediatR;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Vitarish.Identity.Core.Parties.Domain.Events;

/// <summary>
/// Event raised when a tenant indicator is deleted.
/// Contains shadow copy of data needed to compute client ID.
/// </summary>
public sealed record TenantIndicatorDeletedEvent(
    Guid TenantId,
    Guid IndicatorId,
    IndicatorMethod Method,
    string Value) : INotification;
