using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Aggregator.Core.BackgroundJobs;

namespace Vitarish.Communications.Infrastructure.DependencyInjection;

public static class CommunicationsBackgroundJobExtensions
{
    public static IServiceCollection AddCommunicationsBackgroundJobs(this IServiceCollection services)
    {
        services.TryAddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<ICommunicationCommandQueue, HangfireMessageQueue>();
        _ = services.AddTransient<MessageProcessor>();
        _ = services.AddSingleton<HangfireCommunicationsJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, CommunicationsRecurringJobScheduler>();
        return services;
    }
}
