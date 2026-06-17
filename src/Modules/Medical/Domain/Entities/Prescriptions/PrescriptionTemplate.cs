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

    public void Archive(DateTime occurredAt)
    {
        Status = PrescriptionTemplateStatus.Archived;
        UpdatedAt = occurredAt;
    }
}
