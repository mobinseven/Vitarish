namespace Vitarish.Sea.Application.Commands.Events;

public record UpdateEventsChainCommand(Guid ProcedureId, IReadOnlyList<EventChainChange> Events) : IRequest;

public record EventChainChange(
    Guid? Id,
    Guid? PreviousEventId);

