using Microsoft.Extensions.DependencyInjection;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Infrastructure.Services;

namespace Vitarish.Feed.Infrastructure;

public static class FeedServiceExtensions
{
    /// <summary>
    /// Registers feed infrastructure services including FeedService and query handlers.
    /// Call this from your module's registration code.
    /// </summary>
    public static IServiceCollection AddFeedInfrastructure(this IServiceCollection services)
    {
        // Register the core feed service
        _ = services.AddScoped<IFeedService, FeedService>();


        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(FeedServiceExtensions).Assembly));

        return services;
    }
}
