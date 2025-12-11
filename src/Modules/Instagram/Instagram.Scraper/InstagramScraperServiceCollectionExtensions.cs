using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitarish.Instagram.Scraper;

public static class InstagramScraperServiceCollectionExtensions
{
    public static IServiceCollection AddInstagramScraper(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddHttpClient(InstagramScraperHttpClient.Name);

        _ = services.AddSingleton<IInstagramScraperApiClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<InstagramScraperOptions>>().Value;
            var logger = provider.GetRequiredService<ILogger<InstagramScraperClient>>();
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

            return new InstagramScraperClient(options.ApifyToken, httpClientFactory.CreateClient(InstagramScraperHttpClient.Name), logger);
        });

        _ = services.AddScoped<IInstagramScraperService, InstagramScraperService>();

        return services;
    }

    private static class InstagramScraperHttpClient
    {
        internal const string Name = "Instagram.Scraper";
    }
}
