namespace Vitarish.Sea.Application.Commands.Events;

public record CommitProcedureEventsCommand(Guid ProcedureId, Guid ReporterUserId, DateTimeOffset CommitTime) : IRequest<int>;
