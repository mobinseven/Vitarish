using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>Reusable prescription item values copied into draft prescriptions.</summary>
public sealed class PrescriptionTemplateItem : Entity
{
    public Guid PrescriptionTemplateId { get; set; }
    public PrescriptionItemCategory Category { get; set; }
    public string? ReferenceCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? DosageJson { get; set; }
    public int Quantity { get; set; } = 1;
    public int? DurationDays { get; set; }
    public string? MetadataJson { get; set; }
    public int SortOrder { get; set; }

    public static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Template item display name is required.", nameof(displayName));
        }

        return displayName.Trim();
    }
}
