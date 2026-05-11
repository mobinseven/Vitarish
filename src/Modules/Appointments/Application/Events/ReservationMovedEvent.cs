using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationMovedEvent(
    Guid ReservationId,
    Guid FromSessionId,
    DateOnly FromDate,
    Guid NewReservationId,
    Guid ToSessionId,
    DateOnly ToDate,
    string? ReservingPartyId) : INotification;
