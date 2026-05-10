using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record VitalsRecordedEvent(Guid WorkflowId, IReadOnlyList<string> ObservationRefs, string ActorId) : INotification;
