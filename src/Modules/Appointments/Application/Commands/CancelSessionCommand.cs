namespace Vitarish.Appointments.Application.Commands;

public record CancelSessionCommand(Guid SessionId) : IRequest;
