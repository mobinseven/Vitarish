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
        Cron.HourInterval(Math.Clamp(retentionOptions.Value.CleanupIntervalHours, 1, 24)));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
