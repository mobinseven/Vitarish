using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record MedicationRequestedEvent(Guid WorkflowId, string MedicationRequestFhirId, string ActorId) : INotification;
