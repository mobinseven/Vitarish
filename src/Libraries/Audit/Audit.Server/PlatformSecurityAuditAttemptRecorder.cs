using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Audit.Server;

internal sealed class PlatformSecurityAuditAttemptRecorder(
    ISecurityAuditWriter auditWriter,
    IRequestContext requestContext,
    IMultiTenantContextAccessor<TenantInfo>? tenantContextAccessor = null)
{
    public async Task RecordAsync(
        string eventName,
        string operation,
        string idempotencySuffix,
        string outcome,
        CancellationToken cancellationToken)
    {
        if (tenantContextAccessor is not IMultiTenantContextSetter setter)
            return;

        string requestId = requestContext.RequestId ?? Guid.NewGuid().ToString("N");
        IDisposable scope = UsePlatformScope(tenantContextAccessor!);
        try
        {
            string? actorId = requestContext.UserId;
            _ = await auditWriter.WriteAsync(new SecurityAuditEvent
            {
                EventName = eventName,
                EventCategory = SecurityAuditEventCatalog.Find(eventName, operation)?.Category,
                Outcome = outcome,
                ActorKind = string.IsNullOrWhiteSpace(actorId) ? SecurityAuditActorKind.Anonymous : SecurityAuditActorKind.User,
                ActorId = actorId,
                TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
                Resource = SecurityAuditScopeCatalog.PlatformResource,
                Operation = operation,
                OccurredAt = DateTime.UtcNow,
                CorrelationId = requestId,
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                IdempotencyKey = requestId + idempotencySuffix
            }, cancellationToken);
        }
        catch (SecurityAuditStoreException)
        {
            // Preserve the review or export result when its audit attempt cannot be stored.
        }
        finally
        {
            scope.Dispose();
        }
    }

    private static IDisposable UsePlatformScope(IMultiTenantContextAccessor<TenantInfo> accessor)
    {
        if (accessor is not IMultiTenantContextSetter setter)
            throw new InvalidOperationException("The platform audit scope requires a mutable tenant accessor.");

        IMultiTenantContext<TenantInfo> previous = accessor.MultiTenantContext;
        setter.MultiTenantContext = new MultiTenantContext<TenantInfo>
        {
            TenantInfo = new TenantInfo
            {
                Id = SecurityAuditScopeCatalog.PlatformScopeId,
                Identifier = SecurityAuditScopeCatalog.PlatformScopeId,
                Name = SecurityAuditScopeCatalog.PlatformScopeId
            }
        };
        return new Scope(setter, previous);
    }

    private sealed class Scope(IMultiTenantContextSetter setter, IMultiTenantContext<TenantInfo> previous) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            setter.MultiTenantContext = previous;
            disposed = true;
        }
    }
}
