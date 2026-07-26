namespace Vitarish.Medical.Domain.Entities;

public sealed class MedicalCode : Entity
{
    public Guid CodeSystemReleaseId { get; set; }

    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public string? LongDescription { get; set; }

    public MedicalCodeStatus Status { get; set; } = MedicalCodeStatus.Draft;

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public RecordOrigin RecordOrigin { get; set; }

    public string? SourceMetadataJson { get; set; }

    /// <summary>Provider-owned category retained from synchronized reference data.</summary>
    public string? ProviderCategory { get; set; }

    /// <summary>Invariant search projections derived when reference data is written.</summary>
    public string NormalizedCode { get; set; } = string.Empty;
    public string NormalizedDisplayName { get; set; } = string.Empty;
    public string? NormalizedProviderCategory { get; set; }

    public Guid? ImportBatchId { get; set; }
}
