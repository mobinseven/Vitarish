using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Search.Domain.Abstractions;
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
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ElasticSearchOptions esOptions = configuration
            .GetSection(ElasticSearchOptions.SectionName)
            .Get<ElasticSearchOptions>() ?? new ElasticSearchOptions();

        _ = services.AddSingleton(esOptions);

        _ = services.AddSingleton<ElasticsearchClient>(_ =>
        {
            ElasticsearchClientSettings settings = new(new Uri(esOptions.Url));

            if (!string.IsNullOrWhiteSpace(esOptions.Username) &&
                !string.IsNullOrWhiteSpace(esOptions.Password))
            {
                settings = settings.Authentication(
                    new BasicAuthentication(esOptions.Username, esOptions.Password));
            }

            return new ElasticsearchClient(settings);
        });

        _ = services.AddScoped<ISearchService, SearchService>();

        _ = services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(SearchServiceExtensions).Assembly));

        return services;
    }
}
