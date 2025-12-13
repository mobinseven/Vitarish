using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Application.Commands.Events;

public record AddEventAfterCommand(Guid ProjectId, Guid ProcedureId, Guid? AfterEventId) : IRequest<Event?>;
