using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record SessionReminderPolicyTriggeredEvent(
    Guid ReservationId,
    Guid SessionOccurrenceId,
    string? ReservingPartyId,
    ReservationStatus NewStatus,
    DateTimeOffset OccurrenceStart) : INotification;
