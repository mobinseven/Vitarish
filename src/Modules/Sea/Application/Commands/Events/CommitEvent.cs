namespace Vitarish.Sea.Application.Commands.Events;


public record CommitEventCommand(Guid EventId, Guid ReporterUserId, DateTime CommitTime) : IRequest;

