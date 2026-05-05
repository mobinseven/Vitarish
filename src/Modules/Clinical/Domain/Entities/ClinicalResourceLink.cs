namespace Vitarish.Clinical.Domain.Entities;

public class ClinicalResourceLink : Entity
{
    public string LocalEntityType { get; set; } = string.Empty;
    public Guid LocalEntityId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string? ResourceVersionId { get; set; }
    public string? ETag { get; set; }
    public string? OwnerUserId { get; set; }
    public string? PatientReference { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
}
