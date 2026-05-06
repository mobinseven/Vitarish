using Hangfire;
using Vitarish.Appointments.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Appointments;

/// <summary>
/// Hangfire-backed implementation of <see cref="ISessionOccurrenceSyncQueue"/> that enqueues an
/// immediate fire-and-forget job to synchronize session occurrence records.
/// </summary>
public class HangfireSessionOccurrenceSyncQueue(
    IBackgroundJobClient backgroundJobClient,
    IRequestContext requestContext,
    ILogger<HangfireSessionOccurrenceSyncQueue> logger) : ISessionOccurrenceSyncQueue
{
    /// <inheritdoc/>
    public void EnqueueSync(Guid sessionId)
    {
        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) || tenantId == Guid.Empty)
        {
            logger.LogWarning(
                "HangfireSessionOccurrenceSyncQueue: could not enqueue occurrence sync for session '{SessionId}' — tenant ID '{TenantId}' is missing or invalid.",
                sessionId,
                requestContext.TenantId);
            return;
        }

        _ = backgroundJobClient.Enqueue<SessionOccurrenceSyncProcessor>(
            processor => processor.ProcessSyncAsync(tenantId, sessionId, CancellationToken.None));
    }
}
