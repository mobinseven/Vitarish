using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Infrastructure.BackgroundJobs;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Files.Infrastructure.DependencyInjection;

public static class FilesBackgroundJobExtensions
{
    public static IServiceCollection AddFilesBackgroundJobs(this IServiceCollection services)
    {
        _ = services.AddTransient<FilesSelfTestsProcessor>();
        _ = services.AddSingleton<HangfireFilesJobManager>();
        _ = services.AddSingleton<IRecurringJobScheduler, FilesRecurringJobScheduler>();
        return services;
    }
}
