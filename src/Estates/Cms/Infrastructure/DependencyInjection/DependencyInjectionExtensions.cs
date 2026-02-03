namespace Vitarish.Cms.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCmsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishCmsInfrastructure();
        _ = services.AddScoped<EntityInterceptor>();
        _ = services.AddScoped<SessionContextInterceptor>();
        return services;
    }
}
