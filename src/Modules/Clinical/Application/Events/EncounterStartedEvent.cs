using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record EncounterStartedEvent(Guid WorkflowId, string EncounterFhirId, string PatientRef) : INotification;
