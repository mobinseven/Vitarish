namespace Vitarish.Sea.Application.Commands.Events;

public record CommitProcedureEventsCommand(Guid ProcedureId, Guid ReporterUserId, DateTime CommitTime) : IRequest<int>;
