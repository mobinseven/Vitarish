using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record FhirCommitFailedEvent(Guid WorkflowId, string ActionType, string FailureKind, string CorrelationId) : INotification;
