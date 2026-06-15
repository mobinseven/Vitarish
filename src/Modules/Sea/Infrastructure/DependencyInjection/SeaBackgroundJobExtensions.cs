using Vitarish.Modules.Base.BackgroundJobs;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.DependencyInjection;

public static class SeaBackgroundJobExtensions
{
    public static IServiceCollection AddSeaBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<AttachmentMigrationProcessor>();
        _ = services.AddTransient<SeaIndexingProcessor>();
        _ = services.AddSingleton<HangfireSeaJobManager>();
        _ = services.AddSingleton<HangfireSeaIndexingJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, SeaRecurringJobScheduler>();
        return services;
    }
}
