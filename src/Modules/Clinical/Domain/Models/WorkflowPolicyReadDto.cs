namespace Vitarish.Clinical.Domain.Models;

public sealed record WorkflowPolicyReadDto
{
    public Guid Id { get; init; }
    public string VisitType { get; init; } = string.Empty;
    public WorkflowPolicyDefinition? Definition { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
