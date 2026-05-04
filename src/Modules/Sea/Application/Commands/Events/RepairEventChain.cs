namespace Vitarish.Sea.Application.Commands.Events;

public record RepairEventChainCommand(Guid ProcedureId) : IRequest;
