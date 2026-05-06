using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record VisitCompletedEvent(Guid WorkflowId, string EncounterFhirId, string PatientRef, string TenantId) : INotification;
