using Vitarish.Medical.Contracts.Enums;
namespace Vitarish.Medical.Contracts.Imports;

public sealed record MedicalImportResultResponse(Guid ImportBatchId, MedicalImportBatchStatus Status, int RecordsProcessed, int RecordsImported, int RecordsSkipped, IReadOnlyList<string> Warnings, IReadOnlyList<string> Errors);
public sealed record ImportBatchResponse { public Guid Id { get; init; } public MedicalImportBatchStatus Status { get; init; } public MedicalImportSourceType SourceType { get; init; } public string? SourceVersion { get; init; } public DateTime CreatedAt { get; init; } public DateTime ImportedAt { get; init; } public string? ImportedBy { get; init; } public DateTime? CompletedAt { get; init; } public int RecordsProcessed { get; init; } public int RecordsImported { get; init; } public int RecordsSkipped { get; init; } public string? ValidationSummary { get; init; } public string? ErrorReport { get; init; } public IReadOnlyList<string> Warnings { get; init; } = []; public IReadOnlyList<string> Errors { get; init; } = []; }
// TODO: Ensure DateTime is not causing any troubles if client and server were in different timezones. This is ADR002 related issue.
