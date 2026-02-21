using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Modules.Elasticsearch.Application.Abstractions;
using Vitarish.Modules.Elasticsearch.Infrastructure.Configuration;
using Vitarish.Modules.Elasticsearch.Infrastructure.Services;

namespace Vitarish.Modules.Elasticsearch.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddElasticsearchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = ElasticsearchOptions.SectionName)
    {
        _ = services
            .AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .Validate(ValidateAuthentication, "Provide either ApiKey or Username/Password authentication.")
            .ValidateOnStart();

        _ = services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("ElasticsearchTransport");
            var settings = new ElasticsearchClientSettings(options.Uri)
                .RequestTimeout(options.RequestTimeout)
                .PingTimeout(options.PingTimeout)
                .MaximumRetries(options.MaxRetries)
                .MaxRetryTimeout(options.MaxRetryTimeout)
                .OnRequestCompleted(details => LogRequest(details, logger));

            settings = ApplyAuthentication(settings, options.Auth);

            return new ElasticsearchClient(settings);
        });

        _ = services.AddScoped<ISearchIndexNameResolver, SearchIndexNameResolver>();
        _ = services.AddScoped<ISearchIndexService, ElasticsearchIndexService>();
        _ = services.AddScoped<IndexLifecycleService>();
        _ = services.AddScoped<ISearchQueryService, ElasticsearchQueryService>();
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjectionExtensions).Assembly));

        _ = services.AddSingleton<IEntityIndexingMessageQueue, InMemoryEntityIndexingMessageQueue>();
        _ = services.AddHostedService<EntityIndexingProcessor>();

        return services;
    }

    private static bool ValidateAuthentication(ElasticsearchOptions options)
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(options.Auth.ApiKey);
        var hasUsernamePassword = !string.IsNullOrWhiteSpace(options.Auth.Username)
                                  && !string.IsNullOrWhiteSpace(options.Auth.Password);

        return hasApiKey || hasUsernamePassword;
    }

    private static ElasticsearchClientSettings ApplyAuthentication(
        ElasticsearchClientSettings settings,
        ElasticsearchAuthOptions auth)
    {
        if (!string.IsNullOrWhiteSpace(auth.ApiKey))
        {
            return settings.Authentication(new ApiKey(auth.ApiKey));
        }

        return settings.Authentication(new BasicAuthentication(auth.Username!, auth.Password!));
    }

    private static void LogRequest(ApiCallDetails details, ILogger logger)
    {
        var statusCode = details.HttpStatusCode;
        var isFailure = details.OriginalException is not null || statusCode is >= 400;

        if (!isFailure)
        {
            return;
        }

        var retryCount = Math.Max(0, details.AuditTrail.Count() - 1);

        logger.LogWarning(
            details.OriginalException,
            "Elasticsearch request failed. Method={Method}; Uri={Uri}; StatusCode={StatusCode}; Retries={Retries}; Debug={DebugInformation}",
            details.HttpMethod,
            details.Uri,
            statusCode,
            retryCount,
            details.DebugInformation);

        if (retryCount > 0)
        {
            logger.LogInformation("Elasticsearch retried request {Retries} time(s) before failure. Method={Method}; Uri={Uri}", retryCount, details.HttpMethod, details.Uri);
        }
    }
}
