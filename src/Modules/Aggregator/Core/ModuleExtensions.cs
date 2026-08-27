using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Configuration;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;
using BaseModule = Vitarish.Modules.Base.Core.Module;

public static class ModulesExtensions
{
    private static readonly HashSet<string> EvaluatedModuleNames = ["Authentication", "Requests", "Users", "Parties", "Platform", "Feed"];
    private static readonly string[] ModuleAssemblyNames =
    [
        "Vitarish.Authentication.Module",
        "Vitarish.Requests.Module",
        "Vitarish.Users.Module",
        "Vitarish.Parties.Module",
        "Vitarish.Platform.Module",
        "Vitarish.Communications.Module",
        "Vitarish.Intelligence.Module",
        "Vitarish.Knowledge.Module",
        "Vitarish.Files.Module",
        "Vitarish.Financial.Module",
        "Vitarish.Search.Module",
        "Vitarish.Contents.Module",
        "Vitarish.Links.Module",
        "Vitarish.Sea.Module",
        "Vitarish.Appointments.Module",
        "Vitarish.Commerce.Module",
        "Vitarish.Social.Module",
        "Vitarish.Medical.Module",
        "Vitarish.Feed.Module",
        "Vitarish.Journey.Module",
    ];

    public static List<IModule> GetModules(IConfiguration? configuration = null)
    {
        bool evaluated = configuration?.GetValue<bool>(BaseModule.EvaluatedConfigurationEnabledKey) == true;
        string? profile = configuration?[BaseModule.EvaluatedConfigurationIdentityKey];

        if (evaluated && !string.Equals(profile, BaseModule.MainOnlyEvaluatedProfileIdentity, StringComparison.Ordinal) &&
            !string.Equals(profile, BaseModule.ProductEvaluatedProfileIdentity, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unknown evaluated composition profile '{profile ?? ""}'.");

        bool mainOnly = evaluated && string.Equals(profile, BaseModule.MainOnlyEvaluatedProfileIdentity, StringComparison.Ordinal);

        return ModuleAssemblyNames
            .SelectMany(GetModuleTypes)
            .Select(moduleType => (IModule)Activator.CreateInstance(moduleType)!)
            .Where(module => !mainOnly || EvaluatedModuleNames.Contains(module.Name))
            .ToList();
    }

    private static IEnumerable<Type> GetModuleTypes(string assemblyName)
    {
        Assembly assembly;
        try
        {
            assembly = Assembly.Load(new AssemblyName(assemblyName));
        }
        catch (FileNotFoundException)
        {
            return [];
        }

        try
        {
            return assembly.GetTypes()
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IModule).IsAssignableFrom(type))
                .OrderBy(type => type.FullName, StringComparer.Ordinal);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(IModule).IsAssignableFrom(type))
                .OrderBy(type => type!.FullName, StringComparer.Ordinal)!;
        }
    }

    extension(WebApplicationBuilder builder)
    {
        public async Task<WebApplicationBuilder> RegisterModules(
        Action<DbContextOptionsBuilder>? configureDbContext = null)
        {
            List<IModule> Modules = GetModules(builder.Configuration);

            builder.Services.TryAddSingleton<IReadOnlyList<IModule>>(Modules);

            foreach (IModule Module in Modules)
            {
                await Module.Register(builder, configureDbContext);
            }

            builder.Services.AddModuleOpenApiDocuments(Modules.Select(m => m.Name));

            return builder;
        }
    }

    extension(WebApplication app)
    {
        public WebApplication MapModuleEndpoints()
        {
            IReadOnlyList<IModule>? modules = app.Services.GetService<IReadOnlyList<IModule>>();
            if (modules is null)
            {
                return app;
            }

            foreach (IModule module in modules)
            {
                module.MapEndpoints(app);
            }

            return app;
        }

        public async Task<WebApplication> InitializeModules()
        {
            IReadOnlyList<IModule>? Modules = app.Services.GetService<IReadOnlyList<IModule>>();
            if (Modules is null)
            {
                return app;
            }

            foreach (IModule Module in Modules)
            {
                await Module.Initialize(app);
            }

            return app;
        }
    }
}
