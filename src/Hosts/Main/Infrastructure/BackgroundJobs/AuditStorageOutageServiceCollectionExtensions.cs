using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Audit.Application;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

internal static class AuditStorageOutageServiceCollectionExtensions
{
    internal static IServiceCollection AddAuditStorageOutageReporting(this IServiceCollection services, bool openApiGeneration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAuditStorageOutageCounter, RedisAuditStorageOutageCounter>();
        services.TryAddSingleton<PlatformBackgroundAuditRecorder>();
        if (!openApiGeneration)
            _ = services.AddHostedService<AuditStorageOutageReporter>();

        return services;
    }
}
