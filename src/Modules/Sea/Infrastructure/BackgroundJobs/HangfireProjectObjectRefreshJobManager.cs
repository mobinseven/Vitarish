using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

public sealed class HangfireProjectObjectRefreshJobManager(IRecurringJobManager recurringJobManager)
{
    private const string ReconcileProjectObjectViewsJobId = "sea:project-objects:reconcile-views";

    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<ProjectObjectRefreshProcessor>(
        ReconcileProjectObjectViewsJobId,
        processor => processor.ReconcileAsync(CancellationToken.None),
        Cron.MinuteInterval(5));

    public void TriggerProcessing() => recurringJobManager.Trigger(ReconcileProjectObjectViewsJobId);
}
