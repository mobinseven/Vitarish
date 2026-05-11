using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record MoveReservationCommand(Guid ReservationId, Guid TargetSessionId, DateOnly TargetDate) : IRequest;
