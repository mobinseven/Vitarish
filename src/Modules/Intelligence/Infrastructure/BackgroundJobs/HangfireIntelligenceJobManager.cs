using Hangfire;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.BackgroundJobs;

public class HangfireIntelligenceJobManager(
    IRecurringJobManager recurringJobManager,
    IOptions<AgentConversationRetentionOptions> retentionOptions)
{
    private const string JobId = "intelligence:selftests";
    private const string ConversationCleanupJobId = "intelligence:agent-conversation-retention";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<IntelligenceProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.HourInterval(2));

    public void ScheduleConversationCleanup() => recurringJobManager.AddOrUpdate<AgentConversationRetentionCleanup>(
        ConversationCleanupJobId,
        cleanup => cleanup.RunAsync(),
        ConversationCleanupCron(retentionOptions.Value.CleanupIntervalHours));

    // Cron hour steps must be 1-23, so an interval of a day or more runs daily.
    private static string ConversationCleanupCron(int intervalHours) =>
        intervalHours >= 24 ? Cron.Daily() : Cron.HourInterval(Math.Max(intervalHours, 1));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
