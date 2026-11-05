using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Infrastructure.BackgroundJobs;
using Vitarish.Contents.Infrastructure.SearchProviders;
using Vitarish.Contents.Infrastructure.Stores;
using Vitarish.Modules.Base.Core.BackgroundJobs;
using Vitarish.Search.Application.Abstractions;

namespace Vitarish.Contents.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddContentsInfrastructure(this IServiceCollection services)
    {
        _ = services.AddVitarishContentsInfrastructure();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ContentsDbContext).Assembly));
        _ = services.AddScoped<ContentItemsSearchProvider>();
        _ = services.AddScoped<ISearchIndexProvider>(serviceProvider => serviceProvider.GetRequiredService<ContentItemsSearchProvider>());
        _ = services.AddScoped<IContentAuthoringStore, ContentAuthoringStore>();
        services.TryAddScoped<TenantBackgroundExecutor>();
        _ = services.AddTransient<ContentPublicationOutboxProcessor>();
        _ = services.AddSingleton<IRecurringJobScheduler, ContentsRecurringJobScheduler>();

        return services;
    }
}
