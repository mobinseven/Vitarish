namespace Vitarish.Files.Application.Abstractions;

public interface IFileTransferAudit
{
    Task RecordImportAsync(Guid? fileId, string? fileName, long? fileSize, string outcome, string? reason, CancellationToken cancellationToken = default);
    Task RecordExportAsync(Guid fileId, string? fileName, long? fileSize, string outcome, string? reason, CancellationToken cancellationToken = default);
    Task RecordIntegrityVerificationAsync(Guid fileId, string outcome, string? reason, CancellationToken cancellationToken = default);
}
