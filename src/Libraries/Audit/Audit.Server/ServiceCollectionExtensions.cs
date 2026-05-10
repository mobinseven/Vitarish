using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Audit.Application;

namespace Vitarish.Audit.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = services.AddDbContext<AuditDbContext>((_, options) =>
            configureDbContext?.Invoke(options), ServiceLifetime.Transient);

        _ = services.AddTransient<IAuditWriter, AuditWriter>();
        _ = services.AddTransient<IAuditQueryService, AuditQueryService>();

        return services;
    }
}
