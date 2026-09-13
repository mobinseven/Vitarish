using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Sea.Infrastructure.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.DependencyInjection;

public static class SeaBackgroundJobExtensions
{
    public static IServiceCollection AddSeaBackgroundJobs(this IServiceCollection services)
    {
        services.TryAddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<AttachmentMigrationProcessor>();
        _ = services.AddTransient<AttachmentIntegrityVerificationProcessor>();
        _ = services.AddScoped<AttachmentIntegrityVerificationTenantProcessor>();
        _ = services.AddTransient<SeaIndexingProcessor>();
        _ = services.AddSingleton<HangfireSeaJobManager>();
        _ = services.AddSingleton<HangfireSeaIndexingJobManager>();
        _ = services.AddTransient<ReminderProcessor>();
        _ = services.AddSingleton<HangfireRemindersJobManager>();
        _ = services.AddTransient<ProjectObjectRefreshProcessor>();
        _ = services.AddTransient<ProjectObjectRefreshRequestHandler>();
        _ = services.AddSingleton<HangfireProjectObjectRefreshJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, SeaRecurringJobScheduler>();
        return services;
    }
}
