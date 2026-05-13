using MediatR;

namespace Vitarish.Appointments.Application.Events;

public record ReservationMovedEvent(
    Guid ReservationId,
    Guid FromShiftId,
    DateOnly FromDate,
    Guid NewReservationId,
    Guid ToShiftId,
    DateOnly ToDate,
    string? ReservingPartyId) : INotification;
