using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Infrastructure;
using Vitarish.Search.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Search.Interface;

/// <summary>
/// Module entrypoint that wires up search infrastructure and interface services.
/// </summary>
public class SearchModule : Module, ISearchModule
{
    public override string Name => "Search";

    public override Task Register(
        WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ISearchModule, SearchModule>();
        _ = builder.Services.AddSearchInfrastructure(builder.Configuration);
        _ = builder.Services.ConfigureSearchInterface();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
