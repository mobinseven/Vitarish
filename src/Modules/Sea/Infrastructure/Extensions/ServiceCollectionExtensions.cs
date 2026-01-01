using System.Reflection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppMediatR(
        this IServiceCollection services, params Assembly[] assemblies)
    {
        _ = services.AddScoped<SessionContextInterceptor>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(assemblies));
        //_ = services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandFailureNotificationBehavior<,>));
        return services;
    }
}
