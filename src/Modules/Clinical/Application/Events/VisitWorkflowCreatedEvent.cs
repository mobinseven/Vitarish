using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record VisitWorkflowCreatedEvent(Guid WorkflowId, string PatientRef, string VisitType, string TenantId) : INotification;
