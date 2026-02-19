using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Sea.Infrastructure.FeedSources;
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
        
        // Register feed data sources
        _ = services.AddScoped<IFeedDataSource, SeaAttachmentsFeedSource>();
        _ = services.AddScoped<IFeedDataSource, SeaProjectsFeedSource>();
        
        return services;
    }
}
