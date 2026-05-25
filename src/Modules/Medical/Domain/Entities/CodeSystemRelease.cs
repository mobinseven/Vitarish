namespace Vitarish.Medical.Domain.Entities;

public sealed class CodeSystemRelease : Entity
{
    public Guid CodeSystemId { get; set; }

    public required string ReleaseName { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public DateOnly? EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public string? SourceLocator { get; set; }

    public string? SourceVersion { get; set; }

    public Guid? ImportBatchId { get; set; }

    public bool IsActive { get; set; }
}
