using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Communications.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRemindersInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishCommunicationsInfrastructure();
        _ = services.AddEntitySearchInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CommunicationsDbContext).Assembly));
        return services;
    }
}
