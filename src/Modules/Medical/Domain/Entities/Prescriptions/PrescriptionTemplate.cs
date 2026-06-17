using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Domain.Entities.Prescriptions;

/// <summary>Tenant-owned reusable prescription template aggregate.</summary>
public sealed class PrescriptionTemplate : Entity
{
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PrescriptionTemplateStatus Status { get; set; } = PrescriptionTemplateStatus.Active;
    public List<PrescriptionTemplateItem> Items { get; set; } = [];

    public bool IsActive => Status == PrescriptionTemplateStatus.Active;

    public void Rename(string name, string? description, DateTime occurredAt)
    {
        string normalizedName = NormalizeName(name);
        Name = normalizedName;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        UpdatedAt = occurredAt;
    }

    public void Archive(DateTime occurredAt)
    {
        Status = PrescriptionTemplateStatus.Archived;
        UpdatedAt = occurredAt;
    }

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Template name is required.", nameof(name));
        }

        return name.Trim();
    }
}
