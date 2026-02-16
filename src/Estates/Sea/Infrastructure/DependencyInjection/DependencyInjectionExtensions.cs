using Vitarish.Sea.Infrastructure.Services;

namespace Vitarish.Sea.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSeaInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishSeaInfrastructure();
        _ = services.AddTransient<IAccessControlledProjectsService, AccessControlledProjectsService>();
        _ = services.AddTransient<IAccessControlledProceduresService, AccessControlledProceduresService>();
        _ = services.AddTransient<IAccessControlledEventsService, AccessControlledEventsService>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SeaDbContext).Assembly));
        return services;
    }
}
