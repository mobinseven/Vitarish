using MediatR;

namespace Vitarish.Clinical.Application.Events;

public record WorkflowCancelledEvent(Guid WorkflowId, string Reason, string ActorId, string TenantId) : INotification;
