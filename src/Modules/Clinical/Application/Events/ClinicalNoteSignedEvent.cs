using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record ClinicalNoteSignedEvent(Guid WorkflowId, string DocumentFhirId, string ActorId) : INotification;
