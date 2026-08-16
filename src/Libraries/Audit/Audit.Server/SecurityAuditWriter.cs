using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class SecurityAuditWriter(AuditDbContext dbContext) : ISecurityAuditWriter
{
    public async Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        Validate(auditEvent);
        if (auditEvent.Id == Guid.Empty)
        {
            auditEvent.Id = Guid.NewGuid();
        }

        try
        {
            if (await AlreadyRecordedAsync(auditEvent, cancellationToken))
            {
                return SecurityAuditWriteResult.AlreadyRecorded;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new SecurityAuditStoreException("The mandatory security audit store could not be read.", exception);
        }

        await dbContext.SecurityAuditEvents.AddAsync(auditEvent, cancellationToken);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return SecurityAuditWriteResult.Recorded;
        }
        catch (DbUpdateException exception)
        {
            try
            {
                if (await AlreadyRecordedAsync(auditEvent, cancellationToken))
                {
                    return SecurityAuditWriteResult.AlreadyRecorded;
                }
            }
            catch (Exception duplicateCheckException) when (duplicateCheckException is not OperationCanceledException)
            {
                throw new SecurityAuditStoreException("The mandatory security audit store could not confirm a duplicate.", duplicateCheckException);
            }

            throw new SecurityAuditStoreException("The mandatory security audit event could not be persisted.", exception);
        }
    }

    private async Task<bool> AlreadyRecordedAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        dbContext.Entry(auditEvent).State = EntityState.Detached;
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .AnyAsync(existing => existing.TenantId == auditEvent.TenantId && existing.IdempotencyKey == auditEvent.IdempotencyKey, cancellationToken);
    }

    private static void Validate(SecurityAuditEvent auditEvent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.EventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.SourceBoundary);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.IdempotencyKey);
        if (auditEvent.OccurredAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Security audit timestamps must be UTC.", nameof(auditEvent));
        }
    }
}
