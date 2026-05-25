namespace Vitarish.Medical.Domain.Entities;

public sealed class ServiceCode : Entity
{
    public Guid MedicalServiceId { get; set; }

    public Guid CodeSystemReleaseId { get; set; }

    public required string Code { get; set; }

    public string? DisplayName { get; set; }

    public string? ModifierApplicabilityJson { get; set; }

    public bool IsPrimary { get; set; }

    public ServiceCodeStatus Status { get; set; } = ServiceCodeStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public Guid? ImportBatchId { get; set; }
}
