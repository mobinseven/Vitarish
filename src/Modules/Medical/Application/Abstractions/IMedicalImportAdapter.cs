namespace Vitarish.Medical.Application.Abstractions;

public interface IMedicalImportAdapter
{
    ImportSourceType SourceType { get; }
    Task<ImportAdapterResult> ImportAsync(ImportAdapterRequest request, CancellationToken cancellationToken = default);
}

public record ImportAdapterRequest(
    string SourceLocator,
    string SourceVersion,
    bool DryRun = false);

public record ImportAdapterResult(
    bool Success,
    Guid? ImportBatchId,
    int RecordsProcessed,
    int RecordsImported,
    int RecordsSkipped,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
