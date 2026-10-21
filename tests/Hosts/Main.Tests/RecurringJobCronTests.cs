using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Commerce.Infrastructure.DependencyInjection;
using Vitarish.Communications.Infrastructure.DependencyInjection;
using Vitarish.Files.Infrastructure.DependencyInjection;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.DependencyInjection;
using Vitarish.Medical.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Sea.Infrastructure.DependencyInjection;

namespace Main.Tests;

public sealed class RecurringJobCronTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    [InlineData(23)]
    [InlineData(24)]
    [InlineData(48)]
    public void Every_module_recurring_job_has_a_cron_expression_Hangfire_accepts(int? conversationCleanupIntervalHours)
    {
        List<(string Id, Job Job, string Cron)> scheduled = [];
        Mock<IRecurringJobManager> recorder = new();
        recorder
            .Setup(manager => manager.AddOrUpdate(It.IsAny<string>(), It.IsAny<Job>(), It.IsAny<string>(), It.IsAny<RecurringJobOptions>()))
            .Callback<string, Job, string, RecurringJobOptions>((id, job, cron, _) => scheduled.Add((id, job, cron)));

        ServiceCollection services = new();
        _ = services.AddSingleton(recorder.Object);
        AgentConversationRetentionOptions retention = conversationCleanupIntervalHours is { } hours
            ? new() { CleanupIntervalHours = hours }
            : new();
        _ = services.AddSingleton(Options.Create(retention));

        _ = services
            .AddCommerceBackgroundJobs()
            .AddCommunicationsBackgroundJobs()
            .AddFilesBackgroundJobs()
            .AddIntelligenceBackgroundJobs()
            .AddMedicalBackgroundJobs()
            .AddSeaBackgroundJobs();

        using ServiceProvider provider = services.BuildServiceProvider();
        foreach (IRecurringJobScheduler scheduler in provider.GetServices<IRecurringJobScheduler>())
        {
            scheduler.ScheduleJobs();
        }

        scheduled.Should().NotBeEmpty();
        RecurringJobManager hangfire = new(StorageThatStopsAfterValidation());
        foreach ((string id, Job job, string cron) in scheduled)
        {
            Action addOrUpdate = () => hangfire.AddOrUpdate(id, job, cron, new RecurringJobOptions());

            // Hangfire validates the cron expression before it opens a storage connection.
            addOrUpdate.Should().Throw<CronAcceptedException>($"job {id} uses cron '{cron}'");
        }
    }

    private static JobStorage StorageThatStopsAfterValidation()
    {
        Mock<JobStorage> storage = new();
        _ = storage.Setup(jobStorage => jobStorage.GetConnection()).Throws<CronAcceptedException>();
        return storage.Object;
    }

    private sealed class CronAcceptedException : Exception;
}
