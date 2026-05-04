using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitarish.Calendar.Application.Abstractions;
using Vitarish.Calendar.Domain.Entities;
using Vitarish.Calendar.Domain.Models;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Calendar;

/// <summary>
/// Rebuilds occurrence projections for active mappings whose horizon is within the configured threshold.
/// Runs daily per tenant.
/// </summary>
public class OccurrenceProjectionProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    IConfiguration configuration,
    ILogger<OccurrenceProjectionProcessor> logger)
{
    private const string JobName = "hangfire:calendar:projections";
    private const int DefaultHorizonDays = 180;

    /// <summary>
    /// Rebuilds occurrence projections for all active mappings for every tenant.
    /// Invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessProjectionsAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            JobName,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        ICalendarSyncService syncService = provider.GetRequiredService<ICalendarSyncService>();
        IEntityRepository<CalendarObjectMapping, CalendarObjectMappingDescriptor> mappingRepository =
            provider.GetRequiredService<IEntityRepository<CalendarObjectMapping, CalendarObjectMappingDescriptor>>();

        int horizonDays = configuration.GetValue<int?>("Calendar:OccurrenceHorizonDays") ?? DefaultHorizonDays;
        DateTimeOffset horizonEnd = DateTimeOffset.UtcNow.AddDays(horizonDays);

        IReadOnlyList<CalendarObjectMapping> mappings = await mappingRepository.GetAsync(
            new CalendarObjectMappingDescriptor(), shutdownToken);

        foreach (CalendarObjectMapping mapping in mappings)
        {
            shutdownToken.ThrowIfCancellationRequested();

            try
            {
                await syncService.RebuildOccurrenceProjectionAsync(mapping.Id, horizonEnd, shutdownToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to rebuild occurrence projection for mapping '{MappingId}'.", mapping.Id);
            }
        }
    }
}
