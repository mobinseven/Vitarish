namespace Vitarish.Appointments.Application.Commands;

public record UpdateReservationStatusCommand(Guid ReservationId, ReservationStatus Status) : IRequest;
