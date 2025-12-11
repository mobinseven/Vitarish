using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base;

/// <summary>
/// Base implementation for modules providing template methods for common configuration tasks.
/// Derived classes can override specific methods to customize module behavior while maintaining
/// architectural consistency.
/// </summary>
public abstract class ModuleBase : IModule
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        ConfigureServices(builder.Services, builder.Configuration, configureDbContext);
        ConfigureHealthChecks(builder);
        ConfigureSelfTests(builder);
    }

    /// <inheritdoc />
    public async Task MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ConfigureEndpoints(endpoints);
        await ConfigureEndpointsAsync(endpoints);
    }

    /// <summary>
    /// Configures services for dependency injection. Override to add module-specific services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="configureDbContext">Optional delegate to configure database options.</param>
    protected virtual void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {    }

    /// <summary>
    /// Configures HTTP endpoints for the module. Override to map module-specific routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    protected virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <summary>
    /// Configures HTTP endpoints for the module asynchronously.
    /// Override when async initialization is required during endpoint configuration.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A task that completes when async configuration is done.</returns>
    protected virtual Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints) => Task.CompletedTask;

    /// <summary>
    /// Configures health checks for the module. Override to add module-specific health checks.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    protected virtual void ConfigureHealthChecks(WebApplicationBuilder builder)
    {
    }

    /// <summary>
    /// Configures self-tests for the module. Override to register startup validation services.
    /// Self-tests typically run as hosted services to verify module dependencies are available.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    protected virtual void ConfigureSelfTests(WebApplicationBuilder builder)
    {
    }
}
