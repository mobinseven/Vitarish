using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Writes a system audit event in the platform scope on behalf of a background service. The caller decides
/// whether a refused write changes its own control flow; this only owns the scope, request context,
/// timeout and write setup shared by those services.
/// </summary>
internal sealed class PlatformBackgroundAuditRecorder(
    IServiceScopeFactory scopeFactory,
    ILogger<PlatformBackgroundAuditRecorder> logger)
{
    internal async Task<bool> TryRecordAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        try
        {
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IBackgroundRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IBackgroundRequestContextWriter>();
            contextWriter.SetContext(new BackgroundRequestContext(
                TenantBackgroundExecutor.BackgroundAuthority,
                ClientId: null,
                OwnerId: null,
                TenantId: SecurityAuditScopeCatalog.PlatformScopeId,
                ResourceId: auditEvent.Resource,
                RequestId: auditEvent.IdempotencyKey));

            ISecurityAuditWriter auditWriter = scope.ServiceProvider.GetRequiredService<ISecurityAuditWriter>();
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(AuditCapacityAlertOptions.OperationTimeout);
            _ = await auditWriter.WriteAsync(auditEvent, timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Security audit recording failed for a background job ({FailureType}).", exception.GetType().Name);
            return false;
        }
    }
}
