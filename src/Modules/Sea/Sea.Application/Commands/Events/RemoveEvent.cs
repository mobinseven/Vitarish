namespace Vitarish.Sea.Application.Commands.Events;

public record RemoveEventCommand(Guid ProcedureId, Guid EventId) : IRequest<bool>;
