using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class FileTransferAudit(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    FileTransferAuditNameDigest nameDigest,
    ILogger<FileTransferAudit>? logger = null) : IFileTransferAudit
{
    public Task RecordImportAsync(Guid? fileId, string? fileName, long? fileSize, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, fileName, fileSize, SecurityAuditEventCatalog.ImportEvent, SecurityAuditEventCatalog.ImportOperation,
            FileTransferAuditContract.ImportIdempotencySuffix, outcome, reason, cancellationToken);

    public Task RecordExportAsync(Guid fileId, string? fileName, long? fileSize, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, fileName, fileSize, SecurityAuditEventCatalog.ExportEvent, SecurityAuditEventCatalog.ExportOperation,
            FileTransferAuditContract.ExportIdempotencySuffix, outcome, reason, cancellationToken);

    public Task RecordIntegrityVerificationAsync(Guid fileId, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, null, null, SecurityAuditEventCatalog.IntegrityVerificationEvent, SecurityAuditEventCatalog.IntegrityVerificationOperation,
            FileTransferAuditContract.IntegrityVerificationIdempotencySuffix, outcome, reason, cancellationToken);

    private async Task RecordAsync(Guid? fileId, string? fileName, long? fileSize, string eventName, string operation, string suffix, string outcome, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return;
        }

        string? normalizedFileName = FileTransferPolicy.NormalizeFileName(fileName);
        string correlationId = requestContext.TraceId ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.ImportExportCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = requestContext.TenantId,
                Resource = fileId is { } id ? FileTransferAuditContract.ResourcePrefix + id.ToString("N") : null,
                ResourceName = normalizedFileName,
                ResourceNameHash = normalizedFileName is null ? null : nameDigest.Compute(normalizedFileName),
                ResourceExtension = normalizedFileName is null ? null : Extension(normalizedFileName),
                ResourceSize = fileSize,
                Operation = operation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                SourceBoundary = FileTransferAuditContract.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = correlationId + suffix
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // Preserve the initiating transfer result when the audit store is unavailable.
        }
        catch (ArgumentException exception)
        {
            // A rejected metadata value must not replace the transfer result; the event type is the only safe detail.
            logger?.LogError(
                "File transfer audit metadata was rejected. Event={Event} Operation={Operation} Cause={Cause}",
                eventName,
                operation,
                exception.GetType().FullName);
        }
    }

    private static string? Extension(string fileName) =>
        Path.GetExtension(fileName).TrimStart('.') is { Length: > 0 } extension ? extension.ToLowerInvariant() : null;
}
