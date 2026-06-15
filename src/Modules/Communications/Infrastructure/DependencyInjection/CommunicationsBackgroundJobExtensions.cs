using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Communications.Infrastructure.DependencyInjection;

public static class CommunicationsBackgroundJobExtensions
{
    public static IServiceCollection AddCommunicationsBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<ICommunicationCommandQueue, HangfireMessageQueue>();
        _ = services.AddTransient<MessageProcessor>();
        _ = services.AddTransient<ReminderProcessor>();
        _ = services.AddSingleton<HangfireRemindersJobManager>();
        _ = services.AddSingleton<HangfireCommunicationsJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, CommunicationsRecurringJobScheduler>();
        return services;
    }
}
