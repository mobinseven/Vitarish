using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Audit.Application;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Audit.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = services.AddTransientMultiTenantDbContext<AuditDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = services.AddTransient<IAuditWriter, AuditWriter>();
        _ = services.AddTransient<IAuditQueryService, AuditQueryService>();

        return services;
    }
}
