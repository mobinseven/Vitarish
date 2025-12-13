namespace Vitarish.Sea.Application.Commands.Events;

public record UncommitProcedureEventsCommand(Guid ProcedureId) : IRequest<int>;
