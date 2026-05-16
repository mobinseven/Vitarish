using Vitarish.Search.Domain.Abstractions;
using Vitarish.Contents.Infrastructure.SearchProviders;

namespace Vitarish.Contents.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddContentsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishContentsInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ContentsDbContext).Assembly));
        _ = services.AddScoped<ISearchIndexProvider, ContentItemsSearchProvider>();

        return services;
    }
}
