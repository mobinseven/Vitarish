namespace Vitarish.Clinical.Domain.Models;

public sealed record WorkflowStateHistoryReadDto
{
    public Guid Id { get; init; }
    public Guid WorkflowId { get; init; }
    public VisitWorkflowStatus PreviousStatus { get; init; }
    public VisitWorkflowStatus NewStatus { get; init; }
    public string? ActorId { get; init; }
    public string? Reason { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
