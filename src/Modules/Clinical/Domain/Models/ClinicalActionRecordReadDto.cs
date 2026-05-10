namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalActionRecordReadDto
{
    public Guid Id { get; init; }
    public Guid WorkflowId { get; init; }
    public ClinicalActionType ActionType { get; init; }
    public string? ActorId { get; init; }
    public string ValidationStatus { get; init; } = string.Empty;
    public string CommitStatus { get; init; } = string.Empty;
    public string? FhirTransactionReference { get; init; }
    public string? ErrorDetailsJson { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}
