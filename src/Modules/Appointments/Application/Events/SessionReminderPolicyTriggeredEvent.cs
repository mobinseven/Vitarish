using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionReminderPolicyTriggeredEvent(
    Guid ReservationId,
    Guid SessionId,
    DateOnly Date,
    string? ReservingPartyId,
    ReservationStatus NewStatus) : INotification;
