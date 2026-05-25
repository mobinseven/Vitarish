namespace Vitarish.Medical.Domain.Entities;

public sealed class ImportBatch : Entity
{
    public ImportSourceType SourceType { get; set; }

    public required string SourceVersion { get; set; }

    public string? SourceLocator { get; set; }

    public string? SourceChecksum { get; set; }

    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Pending;

    public string? ValidationSummary { get; set; }

    public string? ErrorReport { get; set; }

    public string? ImportedBy { get; set; }

    public DateTimeOffset? ImportedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
