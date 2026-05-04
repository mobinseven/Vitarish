using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Calendar;

/// <summary>
/// Configures the recurring Hangfire jobs for calendar sync and occurrence projection rebuilds.
/// </summary>
public class HangfireCalendarJobManager(
    IRecurringJobManager recurringJobManager,
    IConfiguration configuration)
{
    private const string SyncJobId = "calendar:sync";
    private const string ProjectionsJobId = "calendar:projections";
    private const int DefaultSyncIntervalMinutes = 15;

    /// <summary>
    /// Adds or updates both calendar recurring jobs.
    /// </summary>
    public void ScheduleProcessing()
    {
        int syncIntervalMinutes = configuration.GetValue<int?>("Calendar:SyncIntervalMinutes") ?? DefaultSyncIntervalMinutes;

        recurringJobManager.AddOrUpdate<CalendarSyncProcessor>(
            SyncJobId,
            processor => processor.ProcessSyncAsync(CancellationToken.None),
            $"*/{syncIntervalMinutes} * * * *");

        recurringJobManager.AddOrUpdate<OccurrenceProjectionProcessor>(
            ProjectionsJobId,
            processor => processor.ProcessProjectionsAsync(CancellationToken.None),
            Cron.Daily());
    }

    /// <summary>
    /// Triggers the calendar sync job immediately.
    /// </summary>
    public void TriggerSync()
    {
        recurringJobManager.Trigger(SyncJobId);
    }

    /// <summary>
    /// Triggers the occurrence projection rebuild job immediately.
    /// </summary>
    public void TriggerProjections()
    {
        recurringJobManager.Trigger(ProjectionsJobId);
    }
}
