using Vitarish.Communications.Infrastructure.Data;

namespace Vitarish.Communications.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRemindersInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishCommunicationsInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RemindersDbContext).Assembly));
        return services;
    }
}
