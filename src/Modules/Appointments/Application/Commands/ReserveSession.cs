namespace Vitarish.Appointments.Application.Commands;

public record ReserveSessionCommand(Guid SessionId, string? UserId) : IRequest;
