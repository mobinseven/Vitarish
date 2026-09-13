namespace Vitarish.Files.Domain.Models;

public static class FileTransferAuditContract
{
    public const string SourceBoundary = "files.transfer";
    public const string ResourcePrefix = "files/";
    public const string ImportIdempotencySuffix = ":files-import";
    public const string ExportIdempotencySuffix = ":files-export";
    public const string IntegrityVerificationIdempotencySuffix = ":files-integrity-verification";
}
