using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Vitarish.Modules.Base;

/// <summary>
/// Extension methods for module discovery and registration.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Discovers and registers all modules from the specified assemblies.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <returns>A task that completes when all modules are registered.</returns>
    public static async Task<WebApplicationBuilder> AddModules(
        this WebApplicationBuilder builder,
        params Assembly[] assemblies)
    {
        return await builder.AddModules(assemblies.AsEnumerable());
    }

    /// <summary>
    /// Discovers and registers all modules from the specified assemblies with a shared DbContext configuration delegate.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="configureDbContext">Delegate to configure EF Core database options for modules requiring persistence.</param>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <returns>A task that completes when all modules are registered.</returns>
    public static async Task<WebApplicationBuilder> AddModules(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext,
        params Assembly[] assemblies)
    {
        return await builder.AddModules(assemblies.AsEnumerable(), configureDbContext);
    }

    /// <summary>
    /// Discovers and registers all modules from the specified assemblies.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="assemblies">The assemblies to scan for modules.</param>
    /// <returns>A task that completes when all modules are registered.</returns>
    public static async Task<WebApplicationBuilder> AddModules(
        this WebApplicationBuilder builder,
        IEnumerable<Assembly> assemblies,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        List<IModule> modules = DiscoverModules(assemblies);

        // Use TryAdd to avoid conflicts if modules are registered multiple times
        builder.Services.TryAddSingleton<IReadOnlyList<IModule>>(modules);

        foreach (IModule module in modules)
        {
            await module.Register(builder, configureDbContext);
        }

        return builder;
    }

    /// <summary>
    /// Maps endpoints for all registered modules.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>A task that completes when all module endpoints are mapped.</returns>
    public static async Task<WebApplication> MapModuleEndpoints(this WebApplication app)
    {
        IReadOnlyList<IModule>? modules = app.Services.GetService<IReadOnlyList<IModule>>();
        if (modules is null)
        {
            ILoggerFactory? loggerFactory = app.Services.GetService<ILoggerFactory>();
            ILogger? logger = loggerFactory?.CreateLogger(typeof(ModuleExtensions));
            logger?.LogWarning("No modules registered. Call AddModules() during startup to register modules.");
            return app;
        }

        foreach (IModule module in modules)
        {
            await module.MapEndpoints(app);
        }

        return app;
    }

    /// <summary>
    /// Discovers all module implementations from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="logger">Optional logger for reporting discovery issues.</param>
    /// <returns>A list of instantiated modules.</returns>
    public static List<IModule> DiscoverModules(IEnumerable<Assembly> assemblies, ILogger? logger = null)
    {
        List<IModule> modules = [];

        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t)
                    && !t.IsInterface
                    && !t.IsAbstract
                    && t.GetConstructor(Type.EmptyTypes) != null);

            foreach (Type moduleType in moduleTypes)
            {
                try
                {
                    if (Activator.CreateInstance(moduleType) is IModule module)
                    {
                        modules.Add(module);
                        logger?.LogDebug("Discovered module: {ModuleName} from {AssemblyName}",
                            module.Name, assembly.GetName().Name);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to instantiate module type {ModuleType} from assembly {AssemblyName}",
                        moduleType.FullName, assembly.GetName().Name);
                }
            }
        }

        return modules;
    }
}
