namespace Vitarish.Clinical.Domain.Entities;

public sealed class WorkflowPolicy : Entity
{
    public string VisitType { get; set; } = string.Empty;
    public string PolicyJson { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
