namespace Vitarish.Medical.Application.Contracts;

public record ImportResult(
    Guid ImportBatchId,
    ImportBatchStatus Status,
    int RecordsProcessed,
    int RecordsImported,
    int RecordsSkipped,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
