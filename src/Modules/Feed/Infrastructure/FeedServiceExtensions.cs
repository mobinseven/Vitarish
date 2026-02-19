using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Infrastructure.Services;

namespace Vitarish.Modules.Feed.Infrastructure;

public static class FeedServiceExtensions
{
    /// <summary>
    /// Registers feed infrastructure services including FeedService and query handlers.
    /// Call this from your module's registration code.
    /// </summary>
    public static IServiceCollection AddFeedInfrastructure(this IServiceCollection services)
    {
        // Register the core feed service
        services.AddScoped<IFeedService, FeedService>();


        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FeedServiceExtensions).Assembly));

        return services;
    }
}
