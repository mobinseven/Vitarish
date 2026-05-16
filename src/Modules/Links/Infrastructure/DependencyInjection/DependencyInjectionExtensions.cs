namespace Vitarish.Links.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddLinksInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishLinksInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LinksDbContext).Assembly));

        return services;
    }
}
