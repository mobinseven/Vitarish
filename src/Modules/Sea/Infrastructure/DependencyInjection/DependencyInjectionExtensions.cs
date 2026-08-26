using Vitarish.Entities.Server.Extensions;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Sea.Infrastructure.FeedSources;
using Vitarish.Sea.Infrastructure.SearchProviders;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Search.Application.Abstractions;

namespace Vitarish.Sea.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddSeaInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishSeaInfrastructure();
        _ = services.AddSingleton<IProjectObjectTypeRegistry, ProjectObjectTypeRegistry>();
        _ = services.AddEntitySearchInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SeaDbContext).Assembly));

        // Register feed data sources
        _ = services.AddScoped<IFeedDataSource, SeaAttachmentsFeedSource>();
        _ = services.AddScoped<IFeedDataSource, SeaProjectsFeedSource>();

        // Register search index providers
        _ = services.AddScoped<ISearchIndexProvider, SeaProjectsSearchProvider>();
        _ = services.AddScoped<ISearchIndexProvider, SeaProceduresSearchProvider>();
        _ = services.AddScoped<ISearchIndexProvider, SeaEventsSearchProvider>();
        _ = services.AddScoped<ISearchIndexProvider, SeaAttachmentsSearchProvider>();

        _ = services.AddSeaBackgroundJobs();

        return services;
    }
}
