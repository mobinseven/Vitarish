using System.Data.Common;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

internal sealed class SecurityAuditWriter(
    AuditDbContext dbContext,
    IClientIpAddressAccessor? clientIpAddressAccessor = null,
    ILogger<SecurityAuditWriter>? logger = null,
    IClientInterfaceHostAccessor? clientInterfaceHostAccessor = null) : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
{
    public async Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        dbContext.UseRowScope(tenantId);

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
        ArgumentNullException.ThrowIfNull(auditEvent);
        Normalize(auditEvent);
        Validate(auditEvent);
        dbContext.UseRowScope(auditEvent.TenantId);
        if (auditEvent.Id == Guid.Empty)
            auditEvent.Id = Guid.NewGuid();

        try
        {
            if (await AlreadyRecordedAsync(auditEvent, cancellationToken))
                return SecurityAuditWriteResult.AlreadyRecorded;

            await dbContext.SecurityAuditEvents.AddAsync(auditEvent, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return SecurityAuditWriteResult.Recorded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            try
            {
                if (await AlreadyRecordedAsync(auditEvent, cancellationToken))
                    return SecurityAuditWriteResult.AlreadyRecorded;
            }
            catch (Exception duplicateCheckException) when (duplicateCheckException is not OperationCanceledException)
            {
                SecurityAuditStoreException failure = new("The mandatory security audit store could not confirm a duplicate.", duplicateCheckException);
                LogWriteFailure(auditEvent, duplicateCheckException);
                throw failure;
            }

            SecurityAuditStoreException persistenceFailure = new("The mandatory security audit event could not be persisted.", exception);
            LogWriteFailure(auditEvent, exception);
            throw persistenceFailure;
        }
        catch (Exception exception) when (exception is not SecurityAuditStoreException)
        {
            SecurityAuditStoreException failure = new("The mandatory security audit event could not be persisted.", exception);
            LogWriteFailure(auditEvent, exception);
            throw failure;
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
        dbContext.UseRowScope(tenantId);

        SecurityAuditEvent? pending = null;
        try
        {
            string terminalKey = TerminalKey(tenantId, idempotencyKey);
            if (await dbContext.SecurityAuditEvents.AsNoTracking().AnyAsync(item => item.TenantId == tenantId && item.IdempotencyKey == terminalKey, cancellationToken))
                return SecurityAuditWriteResult.AlreadyRecorded;

            pending = await dbContext.SecurityAuditEvents
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.IdempotencyKey == idempotencyKey && item.Outcome == SecurityAuditOutcome.Pending, cancellationToken);
            if (pending is null)
                return SecurityAuditWriteResult.AlreadyRecorded;

            await dbContext.SecurityAuditEvents.AddAsync(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventName = pending.EventName,
                EventCategory = pending.EventCategory,
                Outcome = outcome,
                ActorKind = pending.ActorKind,
                ActorId = pending.ActorId,
                SubjectId = pending.SubjectId,
                TenantId = pending.TenantId,
                Resource = pending.Resource,
                Operation = pending.Operation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = pending.CorrelationId,
                SourceBoundary = pending.SourceBoundary,
                SourceIpKind = pending.SourceIpKind,
                SourceIpAddress = pending.SourceIpAddress,
                ClientInterfaceHost = pending.ClientInterfaceHost,
                RedactedReason = SecurityAuditRedaction.Apply(reason),
                Changes = outcome == SecurityAuditOutcome.Succeeded ? pending.Changes : null,
                IdempotencyKey = terminalKey
            }, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return SecurityAuditWriteResult.Recorded;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            SecurityAuditStoreException failure = exception as SecurityAuditStoreException
                ?? new SecurityAuditStoreException("The mandatory security audit event could not be updated.", exception);
            LogWriteFailure(pending, exception);
            throw failure;
        }
    }

    private void Normalize(SecurityAuditEvent auditEvent)
    {
        if (auditEvent.Changes is { Count: > 0 } changes)
            auditEvent.Changes = SecurityAuditChangeColumn.Fit(changes);

        auditEvent.EventCategory ??= SecurityAuditEventCatalog.Find(auditEvent.EventName, auditEvent.Operation)?.Category;
        auditEvent.ActorKind ??= auditEvent.ActorId is null ? SecurityAuditActorKind.Anonymous : SecurityAuditActorKind.User;
        auditEvent.RedactedReason = SecurityAuditRedaction.Apply(auditEvent.RedactedReason);

        string? clientIp = clientIpAddressAccessor?.ClientIpAddress;
        if (string.IsNullOrWhiteSpace(auditEvent.SourceIpKind))
        {
            if (IPAddress.TryParse(clientIp, out _))
            {
                auditEvent.SourceIpKind = SecurityAuditSourceKind.Client;
                auditEvent.SourceIpAddress = clientIp;
            }
            else
            {
                auditEvent.SourceIpKind = SecurityAuditSourceKind.Unknown;
                auditEvent.SourceIpAddress = null;
            }
        }
        else if (auditEvent.SourceIpKind == SecurityAuditSourceKind.Client && IPAddress.TryParse(clientIp, out _))
        {
            auditEvent.SourceIpAddress = clientIp;
        }
        else if (auditEvent.SourceIpKind == SecurityAuditSourceKind.Client)
        {
            auditEvent.SourceIpKind = SecurityAuditSourceKind.Unknown;
            auditEvent.SourceIpAddress = null;
        }
        else if (auditEvent.SourceIpKind != SecurityAuditSourceKind.Client)
        {
            auditEvent.SourceIpAddress = null;
        }
        auditEvent.ClientInterfaceHost ??= clientInterfaceHostAccessor?.ClientInterfaceHost;
    }

    private static void Validate(SecurityAuditEvent auditEvent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.EventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.EventCategory);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Outcome);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.ActorKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.TenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.Operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.SourceBoundary);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.SourceIpKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(auditEvent.IdempotencyKey);
        if (auditEvent.ActorKind is SecurityAuditActorKind.Anonymous or SecurityAuditActorKind.System && auditEvent.ActorId is not null)
            throw new ArgumentException("Anonymous and system audit actors cannot have an identity.", nameof(auditEvent));
        if (auditEvent.ActorKind == SecurityAuditActorKind.User && string.IsNullOrWhiteSpace(auditEvent.ActorId))
            throw new ArgumentException("User audit actors require an identity.", nameof(auditEvent));
        if (auditEvent.ActorKind is not SecurityAuditActorKind.User and not SecurityAuditActorKind.Anonymous and not SecurityAuditActorKind.System)
            throw new ArgumentException("Security audit actors must be user, anonymous, or system.", nameof(auditEvent));
        if (auditEvent.SourceIpKind == SecurityAuditSourceKind.Client && !IPAddress.TryParse(auditEvent.SourceIpAddress, out _))
            throw new ArgumentException("Client security audit events require a normalized IP address.", nameof(auditEvent));
        if (auditEvent.SourceIpKind is not SecurityAuditSourceKind.Client and not SecurityAuditSourceKind.System and not SecurityAuditSourceKind.Unknown)
            throw new ArgumentException("Security audit source kinds are invalid.", nameof(auditEvent));
        if (auditEvent.Changes is { } changes &&
            (changes.Count == 0 || !SecurityAuditChangeFields.AreValid(changes) || !SecurityAuditChangeColumn.IsWithinLength(changes)))
        {
            throw new ArgumentException("Security audit changes must use catalogued fields with bounded values.", nameof(auditEvent));
        }
        if (auditEvent.OccurredAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Security audit timestamps must be UTC.", nameof(auditEvent));
    }

    private async Task<bool> AlreadyRecordedAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        dbContext.Entry(auditEvent).State = EntityState.Detached;
        return await dbContext.SecurityAuditEvents.AsNoTracking()
            .AnyAsync(existing => existing.TenantId == auditEvent.TenantId && existing.IdempotencyKey == auditEvent.IdempotencyKey, cancellationToken);
    }

    // Exception messages can carry tenant, user or SQL values, so only the cause's
    // type, the method that threw it and any database SQLSTATE are logged.
    private void LogWriteFailure(SecurityAuditEvent? auditEvent, Exception exception)
    {
        Exception cause = exception;
        string? sqlState = null;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            cause = current;
            sqlState = (current as DbException)?.SqlState ?? sqlState;
        }

        MethodBase? site = cause.TargetSite;
        logger?.LogError(
            "Security audit write unavailable. Code={Code} Category={Category} Event={Event} Operation={Operation} Cause={Cause} Site={Site} SqlState={SqlState}",
            SecurityAuditEventCatalog.WriteFailureCode,
            auditEvent?.EventCategory ?? SecurityAuditRedaction.Replacement,
            auditEvent?.EventName ?? SecurityAuditRedaction.Replacement,
            auditEvent?.Operation ?? SecurityAuditRedaction.Replacement,
            cause.GetType().FullName,
            site is null ? null : $"{site.DeclaringType?.FullName}.{site.Name}",
            sqlState);
    }

    private static string TerminalKey(string tenantId, string idempotencyKey)
    {
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId + '\u001f' + idempotencyKey));
        return SecurityAuditEventCatalog.TerminalIdempotencyPrefix + Convert.ToHexString(digest);
    }
}
