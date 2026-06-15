using Vitarish.Commerce.Application.Abstractions;
using Vitarish.Commerce.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Commerce.Infrastructure.DependencyInjection;

public static class CommerceBackgroundJobExtensions
{
    public static IServiceCollection AddCommerceBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<CommerceCatalogSyncProcessor>();
        _ = services.AddTransient<CommerceOrderSyncProcessor>();
        _ = services.AddTransient<CommerceCartCleanupProcessor>();
        _ = services.AddSingleton<HangfireCommerceJobManager>();
        _ = services.AddSingleton<ICommerceJobManager>(sp => sp.GetRequiredService<HangfireCommerceJobManager>());
        _ = services.AddSingleton<IRecurringJobScheduler, CommerceRecurringJobScheduler>();
        return services;
    }
}
