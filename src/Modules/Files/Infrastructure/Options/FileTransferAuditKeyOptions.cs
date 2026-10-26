namespace Vitarish.Files.Infrastructure.Options;

/// <summary>
/// The secret that keys the file-name digest kept in file transfer audit records. A keyed digest lets a
/// reviewer match two transfers of the same name without anyone being able to recover a guessable name.
/// CurrentKey is supplied by the deployment secret store.
/// </summary>
public sealed class FileTransferAuditKeyOptions
{
    public const string SectionName = "Vitarish:Files:TransferAudit";
    public const int MinimumKeyLength = 32;

    // Development and OpenAPI generation only; a serving non-development host refuses to start without a key.
    internal const string NonProductionKey = "development-file-transfer-audit-key-not-for-deployment";

    public string CurrentKey { get; set; } = string.Empty;
}
