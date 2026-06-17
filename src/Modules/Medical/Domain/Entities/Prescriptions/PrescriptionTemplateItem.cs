using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>Reusable prescription item data owned by a tenant prescription template.</summary>
public sealed class PrescriptionTemplateItem : Entity
{
    public Guid PrescriptionTemplateId { get; set; }
    public PrescriptionItemCategory Category { get; set; }
    public string? ReferenceCode { get; set; }
    public string? DisplayName { get; set; }
    public string? DosageJson { get; set; }
    public int Quantity { get; set; } = 1;
    public int? DurationDays { get; set; }
    public string? MetadataJson { get; set; }
    public int SortOrder { get; set; }
}
