using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Security;

/// <summary>
/// Counts failed authentication attempts per host-normalized client IP address, across every identifier
/// and method, and denies an address that reaches the policy's per-address threshold for the failed-attempt
/// denial duration. A successful sign-in does not reset the count, so one valid account cannot clear the
/// failures an address has made against others. Requests without a client address are not counted.
/// </summary>
public sealed class AuthenticationSourceAttemptGuard(
    IAuthenticationAttemptTracker attemptTracker,
    IAuthenticationAttemptKeyFactory attemptKeyFactory,
    IClientIpAddressAccessor clientIpAddressAccessor,
    ISecurityAuditWriter securityAuditWriter,
    IRequestContext requestContext)
{
    public async Task<bool> IsBlockedAsync(CancellationToken cancellationToken)
    {
        string? address = clientIpAddressAccessor.ClientIpAddress;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        foreach (string key in attemptKeyFactory.SourceCandidates(address))
        {
            if (await attemptTracker.IsDeniedAsync(key, cancellationToken))
                return true;
        }
        return false;
    }

    /// <summary>Records one failed attempt from the current address and returns whether the address is now denied.</summary>
    public async Task<bool> RegisterFailureAsync(SecurityAccessPolicy policy, CancellationToken cancellationToken)
    {
        string? address = clientIpAddressAccessor.ClientIpAddress;
        if (string.IsNullOrWhiteSpace(address))
            return false;

        int failures = 0;
        foreach (string key in attemptKeyFactory.SourceCandidates(address))
        {
            failures = Math.Max(failures, await attemptTracker.RegisterFailureAsync(
                key,
                policy.SourceFailedAttemptThreshold,
                policy.FailedAttemptDenialDuration,
                cancellationToken));
        }
        if (failures < policy.SourceFailedAttemptThreshold)
            return false;

        await WriteBlockedAuditAsync(cancellationToken);
        return true;
    }

    private async Task WriteBlockedAuditAsync(CancellationToken cancellationToken)
    {
        string idempotencyKey = (requestContext.RequestId ?? Guid.NewGuid().ToString("N")) + AuthenticationSecurityAuditEvents.SourceBlockKeySuffix;
        try
        {
            _ = await securityAuditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = AuthenticationSecurityAuditEvents.SourceBlocked,
                Outcome = SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                TenantId = requestContext.TenantId!,
                Resource = AuthenticationSecurityAuditEvents.SourceBlockResource,
                Operation = AuthenticationSecurityAuditEvents.SourceBlockOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestContext.TraceId ?? requestContext.RequestId ?? idempotencyKey,
                SourceBoundary = AuthenticationSecurityAuditEvents.SourceBoundary,
                RedactedReason = AuthenticationSecurityAuditEvents.SourceFailedAttemptThreshold,
                IdempotencyKey = idempotencyKey
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // AU-8 preserves the denial when its audit record cannot be stored.
        }
    }
}
