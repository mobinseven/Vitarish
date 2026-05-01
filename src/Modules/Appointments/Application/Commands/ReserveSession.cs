namespace Vitarish.Appointments.Application.Commands;

public record ReserveSessionCommand(Guid SessionId, DateTimeOffset SessionDateTime) : IRequest;
