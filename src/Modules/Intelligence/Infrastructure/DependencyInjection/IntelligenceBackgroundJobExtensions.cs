using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Intelligence.Infrastructure.DependencyInjection;

public static class IntelligenceBackgroundJobExtensions
{
    public static IServiceCollection AddIntelligenceBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddTransient<IntelligenceProcessor>();
        _ = services.AddSingleton<HangfireIntelligenceJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, IntelligenceRecurringJobScheduler>();
        return services;
    }
}
