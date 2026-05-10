using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record DiagnosisAddedEvent(Guid WorkflowId, string ConditionFhirId, string ActorId) : INotification;
