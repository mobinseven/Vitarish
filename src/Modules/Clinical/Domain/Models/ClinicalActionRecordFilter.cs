namespace Vitarish.Clinical.Domain.Models;

public sealed record ClinicalActionRecordFilter
{
    public Guid? WorkflowId { get; init; }
    public ClinicalActionType? ActionType { get; init; }
    public string? CommitStatus { get; init; }
    public int Take { get; init; } = 50;
}
