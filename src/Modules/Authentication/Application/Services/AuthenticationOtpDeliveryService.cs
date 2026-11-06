using Microsoft.Extensions.Logging;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Services;

public sealed class AuthenticationOtpDeliveryService(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    IAuthenticationUtcClock clock,
    ILogger<AuthenticationOtpDeliveryService> logger)
{
    public Task SendSmsCodeAsync(
        string phoneNumber,
        Func<Task> send) =>
        SendAndAuditAsync(
            send,
            AuthenticationDestinationMasking.Phone(phoneNumber),
            AuthenticationSecurityAuditEvents.OtpSmsSend,
            AuthenticationSecurityAuditEvents.OtpSmsSendOperation);

    public Task SendEmailCodeAsync(
        string email,
        Func<Task> send) =>
        SendAndAuditAsync(
            send,
            AuthenticationDestinationMasking.Email(email),
            AuthenticationSecurityAuditEvents.OtpEmailSend,
            AuthenticationSecurityAuditEvents.OtpEmailSendOperation);

    private async Task SendAndAuditAsync(
        Func<Task> send,
        string maskedRecipient,
        string eventName,
        string operation)
    {
        string tenantId = requestContext.TenantId ?? SecurityAuditScopeCatalog.PlatformScopeId;

        try
        {
            await send();
        }
        catch (Exception exception)
        {
            // The caller answers with a generic provider failure, so this is the
            // operator's only record of why delivery failed. Never log the code.
            logger.LogError("Sign-in code delivery {Operation} to {Recipient} failed with {FailureType}", operation, maskedRecipient, exception.GetType().Name);
            await WriteAuditAsync(
                tenantId,
                maskedRecipient,
                eventName,
                operation,
                SecurityAuditOutcome.Failed,
                AuthenticationSecurityAuditEvents.OtpDeliveryFailure);
            throw;
        }

        await WriteAuditAsync(
            tenantId,
            maskedRecipient,
            eventName,
            operation,
            SecurityAuditOutcome.Succeeded,
            reason: null);
    }

    private async Task WriteAuditAsync(
        string tenantId,
        string maskedRecipient,
        string eventName,
        string operation,
        string outcome,
        string? reason)
    {
        string requestKey = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        string idempotencyKey = requestKey + AuthenticationSecurityAuditEvents.OtpDeliveryIdempotencyPrefix + operation;
        try
        {
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
                Outcome = outcome,
                ActorId = requestContext.UserId,
                TenantId = tenantId,
                Resource = AuthenticationSecurityAuditEvents.OtpDeliveryResourcePrefix + maskedRecipient,
                Operation = operation,
                OccurredAt = clock.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = idempotencyKey
            }, CancellationToken.None);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the OTP delivery result when the audit store is unavailable.
        }
    }

}
