using Hangfire;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Contents.Infrastructure.BackgroundJobs;

public sealed class ContentsRecurringJobScheduler(IRecurringJobManager recurringJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs() => recurringJobManager.AddOrUpdate<ContentPublicationOutboxProcessor>(
        ContentPublicationOutboxProcessor.JobId,
        processor => processor.ProcessAsync(CancellationToken.None),
        Cron.MinuteInterval(1));

    public void TriggerStartupJobs()
    {
    }
}
