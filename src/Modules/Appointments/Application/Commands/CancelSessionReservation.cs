namespace Vitarish.Appointments.Application.Commands;

public record CancelSessionReservationCommand(Guid SessionId, string UserId) : IRequest;
