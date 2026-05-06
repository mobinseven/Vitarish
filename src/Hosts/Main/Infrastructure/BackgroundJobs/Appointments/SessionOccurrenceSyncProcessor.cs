using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Appointments;

/// <summary>
/// Hangfire processor that synchronises occurrence records for a single session within a specific tenant's context.
/// </summary>
public class SessionOccurrenceSyncProcessor(
    IPartiesService partiesService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SessionOccurrenceSyncProcessor> logger)
{
    private const string BackgroundAuthority = "system";

    /// <summary>
    /// Synchronises the occurrence records for <paramref name="sessionId"/> in the tenant identified by
    /// <paramref name="tenantId"/>. Invoked by Hangfire as an immediate queued job.
    /// </summary>
    public async Task ProcessSyncAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        TenantReadDto? tenant = await partiesService.GetTenantAsync(tenantId, cancellationToken);

        if (tenant is null)
        {
            logger.LogWarning("SessionOccurrenceSyncProcessor: tenant '{TenantId}' not found; skipping sync for session '{SessionId}'.", tenantId, sessionId);
            return;
        }

        try
        {
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();

            IRequestContextWriter contextWriter = scope.ServiceProvider.GetRequiredService<IRequestContextWriter>();
            contextWriter.SetContext(
                authority: BackgroundAuthority,
                clientId: tenant.AppId,
                ownerId: tenant.OwnerId.ToString(),
                tenantId: tenant.Id.ToString(),
                resourceId: $"hangfire:appointments:occurrence-sync:{sessionId}",
                requestId: Guid.NewGuid().ToString());

            ISessionOccurrenceResolver resolver = scope.ServiceProvider.GetRequiredService<ISessionOccurrenceResolver>();
            await resolver.SyncOccurrencesAsync(sessionId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SessionOccurrenceSyncProcessor: failed to sync occurrences for session '{SessionId}' in tenant '{TenantId}'.", sessionId, tenantId);
            throw;
        }
    }
}
