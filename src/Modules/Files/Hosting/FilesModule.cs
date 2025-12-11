using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Modules.Base;

namespace Vitarish.Files.Hosting;

/// <summary>
/// Module wiring for the Files stack using the shared module contracts.
/// </summary>
public class FilesModule : ModuleBase
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesModule"/> class.
    /// </summary>
    public FilesModule()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FilesModule"/> class with database configuration.
    /// </summary>
    /// <param name="configureDbContext">Delegate to configure the database context.</param>
    public FilesModule(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    /// <inheritdoc />
    public override string Name => "Files";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = configureDbContext ?? this.configureDbContext;
        if (dbConfigurator is null)
        {
            throw new InvalidOperationException("FilesModule requires a DbContext configuration delegate.");
        }

        FilesModuleConfiguration.ConfigureServices(services, configuration, dbConfigurator);// Placeholder: options binding customizations can be added here as the module evolves.
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints) => FilesModuleConfiguration.ConfigureEndpoints(endpoints);

    /// <inheritdoc />
    protected override Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints) => FilesModuleConfiguration.ConfigureEndpointsAsync(endpoints);

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        // Reserved for Files-specific health checks (e.g., object storage availability).
        builder.Services.AddHealthChecks();

    /// <inheritdoc />
    protected override void ConfigureSelfTests(WebApplicationBuilder builder) => builder.Services.AddHostedService<ObjectStorageStartupSelfTest>();
}
