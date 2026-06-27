using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Aggregator.Core;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Options;
using Vitarish.Search.Infrastructure;
using Vitarish.Search.Api;
using Vitarish.Search.Interface.DependencyInjection;

namespace Vitarish.Search.Module;

/// <summary>
/// Module entrypoint that wires up search infrastructure and interface services.
/// </summary>
public class SearchModule : Vitarish.Modules.Aggregator.Core.Module, ISearchModule
{
    public override string Name => "Search";

    public override void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath)
    {
        _ = services.AddOptions<ElasticSearchOptions>()
            .BindConfiguration(sectionPath)
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Url), $"{sectionPath}:{nameof(ElasticSearchOptions.Url)} is not defined")
            .ValidateOnStart();
    }

    public override Task Register(
        WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        RegisterOptions(builder.Services, builder.Configuration, OptionsSectionPath);
        _ = builder.Services.AddSingleton<ISearchModule, SearchModule>();
        _ = builder.Services.AddSearchInfrastructure();
        _ = builder.Services.ConfigureSearchInterface();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.MapSearchApi();
        return Task.CompletedTask;
    }
}
