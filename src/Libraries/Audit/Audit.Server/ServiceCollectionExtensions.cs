using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Audit.Server;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? configureDbContext = null,
        IConfiguration? configuration = null)
    {
        _ = services.AddTransientMultiTenantDbContext<AuditDbContext>((_, options) => configureDbContext?.Invoke(options));

        OptionsBuilder<SecurityAuditOptions> auditOptions = services.AddOptions<SecurityAuditOptions>();
        if (configuration is not null)
            _ = auditOptions.Bind(configuration.GetSection(SecurityAuditOptions.SectionName));

        _ = services.AddTransient<IAuditWriter, AuditWriter>();
        _ = services.AddTransient<IAuditQueryService, AuditQueryService>();
        _ = services.AddTransient<ISecurityAuditWriter, SecurityAuditWriter>();
        _ = services.AddTransient<ISecurityAuditQueryService, SecurityAuditQueryService>();
        _ = services.AddTransient<AuditReviewRecorder>();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationMiddlewareResultHandler, AuditReviewAuthorizationResultHandler>());
        _ = services.AddTransient<IAuditStorageSizeReader, PostgresAuditStorageSizeReader>();

        return services;
    }
}
