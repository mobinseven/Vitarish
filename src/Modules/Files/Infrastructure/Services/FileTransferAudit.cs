using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class FileTransferAudit(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext) : IFileTransferAudit
{
    public Task RecordImportAsync(Guid? fileId, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, SecurityAuditEventCatalog.ImportEvent, SecurityAuditEventCatalog.ImportOperation,
            FileTransferAuditContract.ImportIdempotencySuffix, outcome, reason, cancellationToken);

    public Task RecordExportAsync(Guid fileId, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, SecurityAuditEventCatalog.ExportEvent, SecurityAuditEventCatalog.ExportOperation,
            FileTransferAuditContract.ExportIdempotencySuffix, outcome, reason, cancellationToken);

    public Task RecordIntegrityVerificationAsync(Guid fileId, string outcome, string? reason, CancellationToken cancellationToken = default) =>
        RecordAsync(fileId, SecurityAuditEventCatalog.IntegrityVerificationEvent, SecurityAuditEventCatalog.IntegrityVerificationOperation,
            FileTransferAuditContract.IntegrityVerificationIdempotencySuffix, outcome, reason, cancellationToken);

    private async Task RecordAsync(Guid? fileId, string eventName, string operation, string suffix, string outcome, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            return;
        }

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
    }
}
