using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;

namespace Vitarish.Audit.Server;

internal sealed class SecurityAuditWriter(AuditDbContext dbContext) : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
{
    public async Task<string?> GetOutcomeAsync(
        string tenantId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        try
        {
            string terminalKey = TerminalKey(tenantId, idempotencyKey);
            string? terminalOutcome = await dbContext.SecurityAuditEvents
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.IdempotencyKey == terminalKey)
                .Select(item => item.Outcome)
                .SingleOrDefaultAsync(cancellationToken);
            if (terminalOutcome is not null)
                return terminalOutcome;

            return await dbContext.SecurityAuditEvents
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId && item.IdempotencyKey == idempotencyKey)
                .Select(item => item.Outcome)
                .SingleOrDefaultAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not SecurityAuditStoreException)
        {
            throw new SecurityAuditStoreException("The mandatory security audit store could not be read.", exception);
        }
    }

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

    public async Task<SecurityAuditWriteResult> UpdateAsync(
        string tenantId,
        string idempotencyKey,
        string outcome,
        string? reason,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(outcome);

        try
        {
            string terminalKey = TerminalKey(tenantId, idempotencyKey);
            SecurityAuditEvent? terminal = await dbContext.SecurityAuditEvents
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.IdempotencyKey == terminalKey, cancellationToken);
            if (terminal is not null)
                return SecurityAuditWriteResult.AlreadyRecorded;

            SecurityAuditEvent? pending = await dbContext.SecurityAuditEvents
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.TenantId == tenantId &&
                           item.IdempotencyKey == idempotencyKey &&
                           item.Outcome == SecurityAuditOutcome.Pending,
                    cancellationToken);
            if (pending is null)
                return SecurityAuditWriteResult.AlreadyRecorded;

            await dbContext.SecurityAuditEvents.AddAsync(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventName = pending.EventName,
                Outcome = outcome,
                ActorId = pending.ActorId,
                SubjectId = pending.SubjectId,
                TenantId = pending.TenantId,
                Resource = pending.Resource,
                Operation = pending.Operation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = pending.CorrelationId,
                SourceBoundary = pending.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = terminalKey
            }, cancellationToken);
            try
            {
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                return SecurityAuditWriteResult.Recorded;
            }
            catch (DbUpdateException exception)
            {
                try
                {
                    bool alreadyCompleted = await dbContext.SecurityAuditEvents.AsNoTracking()
                        .AnyAsync(item => item.TenantId == tenantId && item.IdempotencyKey == terminalKey, cancellationToken);
                    if (alreadyCompleted)
                        return SecurityAuditWriteResult.AlreadyRecorded;
                }
                catch (Exception duplicateCheckException) when (duplicateCheckException is not OperationCanceledException)
                {
                    throw new SecurityAuditStoreException("The mandatory security audit event could not confirm a duplicate completion.", duplicateCheckException);
                }

                throw new SecurityAuditStoreException("The mandatory security audit event could not be completed.", exception);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException and not SecurityAuditStoreException)
        {
            throw new SecurityAuditStoreException("The mandatory security audit event could not be updated.", exception);
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

    private static string TerminalKey(string tenantId, string idempotencyKey)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId + '\u001f' + idempotencyKey));
        return "security-audit-terminal:" + Convert.ToHexString(digest);
    }
}
