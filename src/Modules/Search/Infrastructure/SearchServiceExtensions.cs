using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Options;
using Vitarish.Search.Infrastructure.Services;

namespace Vitarish.Search.Infrastructure;

public static class SearchServiceExtensions
{
    /// <summary>
    /// Registers Search infrastructure services: Elasticsearch client, <see cref="ISearchService"/>,
    /// and MediatR handlers. Call this from your module's registration code.
    /// </summary>
    public static IServiceCollection AddSearchInfrastructure(
        this IServiceCollection services)
    {
        _ = services.AddSingleton<ElasticsearchClient>(sp =>
        {
            ElasticSearchOptions esOptions = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;

            ElasticsearchClientSettings settings = new(new Uri(esOptions.Url));

            if (!string.IsNullOrWhiteSpace(esOptions.Username) &&
                !string.IsNullOrWhiteSpace(esOptions.Password))
            {
                settings = settings.Authentication(
                    new BasicAuthentication(esOptions.Username, esOptions.Password));
            }
            _ = settings.EnableDebugMode();
            return new ElasticsearchClient(settings);
        });

        _ = services.AddScoped<ISearchService, SearchService>();

        _ = services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SearchServiceExtensions).Assembly));

        return services;
    }
}
