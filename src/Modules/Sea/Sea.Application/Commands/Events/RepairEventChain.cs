namespace Vitarish.Sea.Application.Commands.Events;

using System.Linq;

public record RepairEventChainCommand(Guid ProcedureId) : IRequest;
