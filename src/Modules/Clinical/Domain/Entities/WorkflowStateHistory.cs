using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Entities;

public sealed class WorkflowStateHistory : Entity
{
    public Guid WorkflowId { get; set; }
    public VisitWorkflowStatus PreviousStatus { get; set; }
    public VisitWorkflowStatus NewStatus { get; set; }
    public string? ActorId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
