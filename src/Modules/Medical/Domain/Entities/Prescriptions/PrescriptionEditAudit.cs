namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>Immutable clinical snapshot retained when an accepted prescription is amended.</summary>
public sealed class PrescriptionEditAudit : Entity
{
    public Guid PrescriptionRecordId { get; set; }
    public int PreviousVersion { get; set; }
    public string? Reason { get; set; }
    public required string PreviousItemsJson { get; set; }
    public required string ProposedItemsJson { get; set; }
    public string Outcome { get; set; } = "Pending";
    public string TenantId { get; set; } = string.Empty;
}
