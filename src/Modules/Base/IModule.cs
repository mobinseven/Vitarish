using Microsoft.EntityFrameworkCore;

namespace Vitarish.Modules.Base;

/// <summary>
/// Core contract that every module must implement.
/// Provides a stable, lean abstraction for module registration and endpoint mapping.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Gets the unique name identifier for this module.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Registers module services and configuration with the application builder.
    /// Called during application startup to configure dependency injection.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configureDbContext">Optional delegate to configure EF Core database options for modules that require it.</param>
    /// <returns>A task that completes when registration is done.</returns>
    Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null);

    /// <summary>
    /// Maps module endpoints to the application's routing system.
    /// Called after the application is built to configure HTTP endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A task that completes when endpoint mapping is done.</returns>
    Task MapEndpoints(IEndpointRouteBuilder endpoints);
}
