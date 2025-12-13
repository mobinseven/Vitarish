namespace Vitarish.Sea.Application.Commands.Events;

public record MoveEventUpCommand(Guid ProcedureId, Guid EventId) : IRequest<bool>;
