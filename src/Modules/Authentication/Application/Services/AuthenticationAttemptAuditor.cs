using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Services;

public sealed class AuthenticationAttemptAuditor(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    IAuthenticationUtcClock clock)
{
    public async Task RecordAsync(
        AuthenticationMethod method,
        string? subjectId,
        bool succeeded,
        CancellationToken cancellationToken,
        string? failureReason = null)
    {
        (string eventName, string operation) = method switch
        {
            AuthenticationMethod.Phone => (AuthenticationSecurityAuditEvents.PhoneLogin, AuthenticationSecurityAuditEvents.PhoneLoginOperation),
            AuthenticationMethod.Email => (AuthenticationSecurityAuditEvents.EmailLogin, AuthenticationSecurityAuditEvents.EmailLoginOperation),
            AuthenticationMethod.Passkey => (AuthenticationSecurityAuditEvents.PasskeyLogin, AuthenticationSecurityAuditEvents.PasskeyLoginOperation),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "This attempt method uses a dedicated audit handler.")
        };

        string requestKey = requestContext.RequestId ?? requestContext.TraceId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
                Outcome = succeeded ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                SubjectId = subjectId,
                TenantId = requestContext.TenantId ?? SecurityAuditScopeCatalog.PlatformScopeId,
                Resource = AuthenticationSecurityAuditEvents.Resource,
                Operation = operation,
                OccurredAt = clock.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = succeeded ? null : failureReason ?? SecurityAuditReasonCatalog.AuthenticationFailed,
                IdempotencyKey = requestKey + AuthenticationSecurityAuditEvents.AuthenticationAttemptIdempotencySuffix + operation
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the authentication result when the audit store is unavailable.
        }
    }
}
