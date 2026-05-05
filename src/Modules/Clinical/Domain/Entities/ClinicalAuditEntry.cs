namespace Vitarish.Clinical.Domain.Entities;

public class ClinicalAuditEntry : Entity
{
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Reason { get; set; }
    public string? Outcome { get; set; }
    public string? DetailsJson { get; set; }
}
