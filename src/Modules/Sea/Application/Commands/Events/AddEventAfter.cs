namespace Vitarish.Sea.Application.Commands.Events;

public record AddEventAfterCommand(
    Guid ProjectId,
    Guid ProcedureId,
    Guid? AfterEventId,
    string Title,
    string Description) : IRequest<Event?>;
