namespace Vitarish.Sea.Application.Commands.Events;

public record MoveEventDownCommand(Guid ProcedureId, Guid EventId) : IRequest<bool>;
