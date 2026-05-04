using MediatR;

namespace Vitarish.Appointments.Application.Commands;

public record UpdateSessionStatusCommand(Guid SessionId, SessionStatus Status) : IRequest;
