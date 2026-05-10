using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record VisitCheckedInEvent(Guid WorkflowId, string PatientRef, string ActorId) : INotification;
