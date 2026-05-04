using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record MoveReservationCommand(Guid ReservationId, Guid NewSessionOccurrenceId) : IRequest;
