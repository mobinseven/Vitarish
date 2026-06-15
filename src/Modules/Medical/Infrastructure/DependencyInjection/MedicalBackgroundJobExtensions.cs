using Vitarish.Medical.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Medical.Infrastructure.DependencyInjection;

public static class MedicalBackgroundJobExtensions
{
    public static IServiceCollection AddMedicalBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<PrescriptionRetryProcessor>();
        _ = services.AddSingleton<HangfirePrescriptionJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, MedicalRecurringJobScheduler>();
        return services;
    }
}
