namespace Vitarish.Clinical.Domain.Models;

public sealed record WorkflowPolicyWriteDto
{
    public required string VisitType { get; init; }
    public required WorkflowPolicyDefinition Definition { get; init; }
    public DateTimeOffset EffectiveFrom { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EffectiveTo { get; init; }
    public bool IsActive { get; init; } = true;
}
