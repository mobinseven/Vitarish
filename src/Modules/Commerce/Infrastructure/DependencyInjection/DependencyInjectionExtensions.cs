using Vitarish.Commerce.Infrastructure.Clients;
using Vitarish.Commerce.Infrastructure.FeedSources;
using Vitarish.Commerce.Infrastructure.SearchProviders;
using Vitarish.Commerce.Infrastructure.Services;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Search.Domain.Abstractions;

namespace Vitarish.Commerce.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCommerceInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishCommerceInfrastructure();
        _ = services.AddTransient<IAccessControlledProductService, AccessControlledProductService>();
        _ = services.AddTransient<IAccessControlledOrderService, AccessControlledOrderService>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CommerceDbContext).Assembly));

        // HTTP clients
        _ = services.AddHttpClient<MedusaAdminClient>();
        _ = services.AddHttpClient<MedusaStorefrontClient>();

        // Search
        _ = services.AddScoped<ISearchIndexProvider, CommerceProductsSearchProvider>();

        // Feed
        _ = services.AddScoped<IFeedDataSource, CommerceFeedSource>();

        return services;
    }
}
