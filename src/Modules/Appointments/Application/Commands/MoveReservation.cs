namespace Vitarish.Appointments.Application.Commands;

public record MoveReservationCommand(Guid ReservationId, Guid TargetShiftId, DateOnly TargetDate) : IRequest;
