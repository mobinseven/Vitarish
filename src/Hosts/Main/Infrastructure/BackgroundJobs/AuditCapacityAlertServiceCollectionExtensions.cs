using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vitarish.Main.Infrastructure.Options;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

internal static class AuditCapacityAlertServiceCollectionExtensions
{
    internal static IServiceCollection AddAuditCapacityAlertMonitoring(
        this IServiceCollection services,
        IHostEnvironment environment,
        bool openApiGeneration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(environment);

        _ = services.AddSingleton<IValidateOptions<AuditCapacityAlertOptions>>(
            new AuditCapacityAlertOptionsValidator(environment, openApiGeneration));
        _ = services.AddOptions<AuditCapacityAlertOptions>()
            .BindConfiguration(AuditCapacityAlertOptions.SectionName)
            .ValidateOnStart();

        if (!openApiGeneration)
            _ = services.AddHostedService<AuditCapacityAlertMonitor>();

        return services;
    }
}
