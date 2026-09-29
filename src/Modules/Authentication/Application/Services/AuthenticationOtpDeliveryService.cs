using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Services;

public sealed class AuthenticationOtpDeliveryService(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    IAuthenticationUtcClock clock)
{
    public Task SendSmsCodeAsync(
        string phoneNumber,
        Func<Task> send) =>
        SendAndAuditAsync(
            send,
            MaskPhoneNumber(phoneNumber),
            AuthenticationSecurityAuditEvents.OtpSmsSend,
            AuthenticationSecurityAuditEvents.OtpSmsSendOperation);

    public Task SendEmailCodeAsync(
        string email,
        Func<Task> send) =>
        SendAndAuditAsync(
            send,
            MaskEmail(email),
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
        catch
        {
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

    private static string MaskPhoneNumber(string phoneNumber)
    {
        string lastDigits = new(phoneNumber.Where(char.IsDigit).TakeLast(4).ToArray());
        return $"*******{lastDigits}";
    }

    private static string MaskEmail(string email)
    {
        int atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return "***";

        string localPart = email[..atIndex];
        string suffix = new(localPart.Where(char.IsLetterOrDigit).TakeLast(2).ToArray());
        return suffix.Length == 0 ? "***@***" : $"***{suffix}@***";
    }
}
