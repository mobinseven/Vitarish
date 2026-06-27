using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Modules.Aggregator.Core.BackgroundJobs;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.DependencyInjection;

public static class SeaBackgroundJobExtensions
{
    public static IServiceCollection AddSeaBackgroundJobs(this IServiceCollection services)
    {
        services.TryAddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<AttachmentMigrationProcessor>();
        _ = services.AddTransient<SeaIndexingProcessor>();
        _ = services.AddSingleton<HangfireSeaJobManager>();
        _ = services.AddSingleton<HangfireSeaIndexingJobManager>();
        _ = services.AddTransient<ReminderProcessor>();
        _ = services.AddSingleton<HangfireRemindersJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, SeaRecurringJobScheduler>();
        return services;
    }
}
