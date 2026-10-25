using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

/// <summary>
/// Records an unauthorized attempt to read the security audit at the review API boundary.
/// The review query itself stays side-effect-free, and authorized reads are not recorded.
/// </summary>
internal static class AuditReviewDenialAudit
{
    public static async Task RecordAsync(ISecurityAuditWriter writer, IRequestContext requestContext, string operationId)
    {
        string correlationId = requestContext.TraceId ?? requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        try
        {
            _ = await writer.WriteAsync(new SecurityAuditEvent
            {
                EventName = SecurityAuditEventCatalog.AuditReviewEvent,
                EventCategory = SecurityAuditEventCatalog.ReviewCategory,
                Outcome = SecurityAuditOutcome.Denied,
                ActorId = requestContext.UserId,
                SubjectId = requestContext.UserId,
                TenantId = string.IsNullOrWhiteSpace(requestContext.TenantId) ? SecurityAuditScopeCatalog.PlatformScopeId : requestContext.TenantId,
                Resource = SecurityAuditEventCatalog.EndpointResourcePrefix + operationId,
                Operation = SecurityAuditEventCatalog.AuditReviewOperation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                RedactedReason = SecurityAuditReasonCatalog.Unauthorized,
                IdempotencyKey = $"{correlationId}:{operationId}{SecurityAuditEventCatalog.AuditReviewIdempotencySuffix}"
            }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (SecurityAuditStoreException)
        {
            // The denial stands when the audit store is unavailable; the writer emits the redacted host error.
        }
    }
}
