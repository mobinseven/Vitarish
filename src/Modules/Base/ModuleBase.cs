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

    public bool IsAvailable
    {
        get;
        set
        {
            if (field != value)
            {
                AvailabilityChanged?.Invoke(value);
            }
            field = value;
        }
    }
    public event Action<bool>? AvailabilityChanged;
    /// <inheritdoc />
    public async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null) => ConfigureServices(builder.Services, builder.Configuration, configureDbContext);

    /// <inheritdoc />
    public async Task MapEndpoints(WebApplication app)
    {
        ConfigureEndpoints(app);
        await InitializeAsync(app);
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
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    { }
    protected virtual void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration) => ConfigureServices(services, configuration, null);

    /// <summary>
    /// Configures HTTP app for the module. Override to map module-specific routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    protected virtual void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    /// <summary>
    /// Initializes the module.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>A task that completes when async configuration is done.</returns>
    protected virtual Task InitializeAsync(WebApplication app) => Task.CompletedTask;
}
