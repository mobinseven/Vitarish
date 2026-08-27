using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Search.Agents;
using Vitarish.Search.Domain.Options;
using Vitarish.Search.Infrastructure;
using Vitarish.Search.Api;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Search.Module;

/// <summary>
/// Module entrypoint that wires up search infrastructure and interface services.
/// </summary>
public class SearchModule : Modules.Base.Core.Module, ISearchModule
{
    public override string Name => "Search";

    public override void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath)
    {
        bool evaluatedProfile = configuration.GetValue<bool>(Vitarish.Modules.Base.Core.Module.EvaluatedConfigurationEnabledKey);
        _ = services.AddOptions<ElasticSearchOptions>()
            .BindConfiguration(sectionPath)
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Url), $"{sectionPath}:{nameof(ElasticSearchOptions.Url)} is not defined")
            .Validate(options => !evaluatedProfile ||
                Uri.TryCreate(options.Url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps,
                $"{sectionPath}:{nameof(ElasticSearchOptions.Url)} must be an absolute HTTPS URI in the evaluated profile.")
            .ValidateOnStart();
        _ = services.AddOptions<SearchAgentOptions>()
            .BindConfiguration($"{sectionPath}:Agent")
            .Validate(options => Enum.IsDefined(options.IndexScope),
                $"{sectionPath}:Agent has an unsupported index scope.")
            .Validate(options => options.IndexScope != SearchAgentIndexScope.AllowList ||
                (options.AllowedIndexes is not null && options.AllowedIndexes.Any(index => !string.IsNullOrWhiteSpace(index))),
                $"{sectionPath}:Agent requires at least one allowed index when IndexScope is AllowList.")
            .ValidateOnStart();
    }

    public override Task Register(
        WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        RegisterOptions(builder.Services, builder.Configuration, OptionsSectionPath);
        _ = builder.Services.AddSingleton<ISearchModule, SearchModule>();
        _ = builder.Services.AddSearchInfrastructure();
        _ = builder.Services.AddScoped<ISearchAgentTool>(serviceProvider =>
            new SearchAgentTool(
                serviceProvider.GetRequiredService<ISearchService>(),
                serviceProvider.GetRequiredService<IOptions<SearchAgentOptions>>().Value));

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        return Task.CompletedTask;
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapSearchApi();
}
