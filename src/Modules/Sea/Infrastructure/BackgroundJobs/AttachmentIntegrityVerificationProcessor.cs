using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Resumptively enrolls and rechecks one attachment integrity record per tenant.
/// </summary>
public sealed class AttachmentIntegrityVerificationProcessor(TenantBackgroundExecutor tenantBackgroundExecutor)
{
    private const string BackgroundResource = "hangfire:sea:attachments:integrity-verification";

    public Task ProcessAsync(CancellationToken cancellationToken = default) =>
        tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            static (provider, token) => provider.GetRequiredService<AttachmentIntegrityVerificationTenantProcessor>().ProcessAsync(token),
            cancellationToken);
}

public sealed class AttachmentIntegrityVerificationTenantProcessor(
    SeaDbContext dbContext,
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    ILogger<AttachmentIntegrityVerificationTenantProcessor> logger)
{
    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        string tenantId = RequireTenantId();
        Attachment? attachment = await dbContext.Attachments
            .IgnoreQueryFilters()
            .Where(candidate => EF.Property<string>(candidate, SeaDbContext.TenantIdPropertyName) == tenantId)
            .Where(candidate => !dbContext.AttachmentIntegrityRecords.Any(record => record.AttachmentId == candidate.Id))
            .OrderBy(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        AttachmentIntegrityRecord? record = null;

        if (attachment is null)
        {
            record = await dbContext.AttachmentIntegrityRecords
                .Where(candidate => candidate.State == AttachmentIntegrityState.Verified)
                .OrderBy(candidate => candidate.VerifiedAt)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (record is not null)
            {
                attachment = await dbContext.Attachments
                    .FirstOrDefaultAsync(candidate => candidate.Id == record.AttachmentId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (attachment is null)
        {
            if (record is not null)
            {
                try
                {
                    record.MarkUnavailable();
                    _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await RecordAuditAsync(record.AttachmentId, false, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Sea attachment integrity verification could not mark missing attachment {AttachmentId} unavailable.", record.AttachmentId);
                    await RecordAuditAsync(record.AttachmentId, false, CancellationToken.None).ConfigureAwait(false);
                }
            }

            return;
        }

        try
        {
            string digest = AttachmentIntegrityProjection.ComputeDigest(attachment);
            bool verified;
            if (record is null)
            {
                record = AttachmentIntegrityRecord.Create(attachment.Id, RequireTenantId(), digest);
                record.MarkVerified(DateTime.UtcNow);
                _ = await dbContext.AttachmentIntegrityRecords.AddAsync(record, cancellationToken).ConfigureAwait(false);
                verified = true;
            }
            else if (record.ProjectionVersion == AttachmentIntegrityProjection.Version &&
                     string.Equals(record.Digest, digest, StringComparison.Ordinal))
            {
                record.MarkVerified(DateTime.UtcNow);
                verified = true;
            }
            else
            {
                record.MarkUnavailable();
                verified = false;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await RecordAuditAsync(attachment.Id, verified, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sea attachment integrity verification could not process attachment {AttachmentId}.", attachment.Id);
            await RecordAuditAsync(attachment.Id, false, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task RecordAuditAsync(Guid attachmentId, bool verified, CancellationToken cancellationToken)
    {
        string correlationId = requestContext.TraceId ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.IntegrityVerificationEvent,
                EventCategory = SecurityAuditEventCatalog.ImportExportCategory,
                Outcome = verified ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Failed,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = RequireTenantId(),
                Resource = AttachmentIntegrityAuditContract.ResourcePrefix + attachmentId.ToString("N"),
                Operation = SecurityAuditEventCatalog.IntegrityVerificationOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                SourceBoundary = AttachmentIntegrityAuditContract.SourceBoundary,
                RedactedReason = verified ? null : SecurityAuditReasonCatalog.IntegrityValidationFailed,
                IdempotencyKey = correlationId + AttachmentIntegrityAuditContract.IdempotencySuffix
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // The existing audit-unavailable policy preserves the integrity result.
        }
    }

    private string RequireTenantId() => requestContext.TenantId ?? throw new InvalidOperationException("Attachment integrity verification requires a tenant.");
}
