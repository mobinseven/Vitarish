using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Users.Application.Security;

public sealed class UsersManagementAudit(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    TimeProvider timeProvider)
{
    public Task AccountCreatedAsync(string userId, CancellationToken cancellationToken) =>
        AccountCreatedAsync(userId, null, cancellationToken);

    public Task AccountCreatedAsync(string userId, IReadOnlyList<SecurityAuditChange>? changes, CancellationToken cancellationToken) =>
        WriteAsync(
            UsersSecurityAuditEvents.UserCreation,
            UsersSecurityAuditEvents.UserCreationOperation,
            UsersSecurityAuditEvents.UserCreationIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.ManagementCategory,
            SecurityAuditOutcome.Succeeded,
            null,
            cancellationToken,
            changes);

    public Task AccountCreationFailedAsync(string? userId, CancellationToken cancellationToken) =>
        AccountCreationFailedAsync(userId, SecurityAuditReasonCatalog.MutationRejected, cancellationToken);

    public Task AccountCreationFailedAsync(string? userId, string reason, CancellationToken cancellationToken) =>
        WriteAsync(
            UsersSecurityAuditEvents.UserCreationFailure,
            UsersSecurityAuditEvents.UserCreationFailureOperation,
            UsersSecurityAuditEvents.UserCreationFailureIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.SecurityFailureCategory,
            SecurityAuditOutcome.Denied,
            reason,
            cancellationToken);

    public Task AccountEditedAsync(string userId, string? securityAttributeChangeReason, CancellationToken cancellationToken) =>
        AccountEditedAsync(userId, securityAttributeChangeReason, null, cancellationToken);

    public async Task AccountEditedAsync(string userId, string? securityAttributeChangeReason, IReadOnlyList<SecurityAuditChange>? changes, CancellationToken cancellationToken)
    {
        await WriteAsync(
            UsersSecurityAuditEvents.UserEdit,
            UsersSecurityAuditEvents.UserEditOperation,
            UsersSecurityAuditEvents.UserEditIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.ManagementCategory,
            SecurityAuditOutcome.Succeeded,
            null,
            cancellationToken,
            changes);

        if (securityAttributeChangeReason is not null)
        {
            await WriteAsync(
                UsersSecurityAuditEvents.UserSecurityAttributeChange,
                UsersSecurityAuditEvents.UserSecurityAttributeChangeOperation,
                UsersSecurityAuditEvents.UserSecurityAttributeChangeIdempotencySuffix,
                userId,
                SecurityAuditEventCatalog.SecurityAttributeCategory,
                SecurityAuditOutcome.Succeeded,
                securityAttributeChangeReason,
                cancellationToken);
        }
    }

    public Task AccountEditFailedAsync(string? userId, string? reason, CancellationToken cancellationToken) =>
        WriteAsync(
            UsersSecurityAuditEvents.UserEditFailure,
            UsersSecurityAuditEvents.UserEditFailureOperation,
            UsersSecurityAuditEvents.UserEditFailureIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.SecurityFailureCategory,
            SecurityAuditOutcome.Denied,
            reason ?? SecurityAuditReasonCatalog.MutationRejected,
            cancellationToken);

    public Task AccountStatusChangedAsync(
        string? userId,
        bool enabled,
        string outcome,
        string? reason,
        CancellationToken cancellationToken,
        bool statusChanged = false) =>
        WriteAsync(
            enabled ? UsersSecurityAuditEvents.UserAccountEnabled : UsersSecurityAuditEvents.UserAccountDisabled,
            enabled ? UsersSecurityAuditEvents.UserAccountEnableOperation : UsersSecurityAuditEvents.UserAccountDisableOperation,
            enabled ? UsersSecurityAuditEvents.UserAccountEnableIdempotencySuffix : UsersSecurityAuditEvents.UserAccountDisableIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.ManagementCategory,
            outcome,
            reason,
            cancellationToken,
            statusChanged
                ? [new SecurityAuditChange(
                    SecurityAuditChangeFields.AccountStatus,
                    enabled ? SecurityAuditChangeFields.AccountStatusDisabled : SecurityAuditChangeFields.AccountStatusEnabled,
                    enabled ? SecurityAuditChangeFields.AccountStatusEnabled : SecurityAuditChangeFields.AccountStatusDisabled)]
                : null);

    public Task SecurityAttributeChangedAsync(string userId, string outcome, string reason, CancellationToken cancellationToken) =>
        WriteAsync(
            UsersSecurityAuditEvents.UserSecurityAttributeChange,
            UsersSecurityAuditEvents.UserSecurityAttributeChangeOperation,
            UsersSecurityAuditEvents.UserSecurityAttributeChangeIdempotencySuffix,
            userId,
            SecurityAuditEventCatalog.SecurityAttributeCategory,
            outcome,
            reason,
            cancellationToken);

    private async Task WriteAsync(
        string eventName,
        string operation,
        string idempotencySuffix,
        string? userId,
        string category,
        string outcome,
        string? reason,
        CancellationToken cancellationToken,
        IReadOnlyList<SecurityAuditChange>? changes = null)
    {
        string requestKey = requestContext.RequestId ?? requestContext.TraceId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = category,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                SubjectId = userId,
                TenantId = requestContext.TenantId ?? UsersSecurityAuditEvents.GlobalScope,
                Resource = string.IsNullOrWhiteSpace(userId)
                    ? UsersSecurityAuditEvents.UnknownUserResource
                    : UsersSecurityAuditEvents.UserResourcePrefix + userId,
                Operation = operation,
                OccurredAt = timeProvider.GetUtcNow().UtcDateTime,
                CorrelationId = requestContext.TraceId ?? requestKey,
                SourceBoundary = UsersSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                Changes = changes is { Count: > 0 } ? changes : null,
                IdempotencyKey = requestKey + idempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // The audit writer emits the redacted host error; AU-8 preserves the user operation.
        }
    }
}
