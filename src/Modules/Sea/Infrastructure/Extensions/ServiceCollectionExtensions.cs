using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Vitarish.Sea.Infrastructure.Behaviors;
using Vitarish.Sea.Infrastructure.Data;

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
