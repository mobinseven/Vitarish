using Vitarish.Reminders.Infrastructure.Data;

namespace Vitarish.Reminders.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddRemindersInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishRemindersInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RemindersDbContext).Assembly));
        return services;
    }
}
