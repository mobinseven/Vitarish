using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Sea.Hosting;

/// <summary>
/// Module wiring for the Sea stack using the shared module contracts.
/// </summary>
public sealed class SeaModule : ModuleBase
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaModule"/> class.
    /// </summary>
    public SeaModule()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaModule"/> class with database configuration.
    /// </summary>
    /// <param name="configureDbContext">Delegate to configure the database context.</param>
    public SeaModule(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    /// <inheritdoc />
    public override string Name => "Sea";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = configureDbContext ?? this.configureDbContext;
        if (dbConfigurator is null)
        {
            throw new InvalidOperationException("SeaModule requires a DbContext configuration delegate.");
        }

        SeaModuleConfiguration.ConfigureServices(services, configuration, dbConfigurator);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints) =>
        SeaModuleConfiguration.ConfigureEndpoints(endpoints);

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        SeaModuleConfiguration.ConfigureHealthChecks(builder.Services);
}
