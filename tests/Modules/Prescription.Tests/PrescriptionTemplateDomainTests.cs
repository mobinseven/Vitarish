using Vitarish.Medical.Domain.Entities.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Modules.Prescription.Tests;

public sealed class PrescriptionTemplateDomainTests
{
    [Fact]
    public void Archive_MovesTemplateOutOfActiveLifecycle()
    {
        PrescriptionTemplate template = new()
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-a",
            Name = "Routine visit",
            Status = PrescriptionTemplateStatus.Active
        };
        DateTime archivedAt = DateTime.UtcNow;

        template.Archive(archivedAt);

        template.Status.Should().Be(PrescriptionTemplateStatus.Archived);
        template.IsActive.Should().BeFalse();
        template.UpdatedAt.Should().Be(archivedAt);
    }

    [Fact]
    public void TemplateItem_DoesNotReferenceDraftPrescriptionState()
    {
        PrescriptionTemplateItem item = new()
        {
            Id = Guid.NewGuid(),
            PrescriptionTemplateId = Guid.NewGuid(),
            Category = PrescriptionItemCategory.Drug,
            ReferenceCode = "RX-1",
            DisplayName = "Reusable drug",
            Quantity = 1,
            SortOrder = 1
        };

        item.PrescriptionTemplateId.Should().NotBeEmpty();
        typeof(PrescriptionTemplateItem).GetProperty("PrescriptionRecordId").Should().BeNull();
    }
}
