using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Vitarish.Modules.Base;

public static class ModulesExtensions
{
    private static readonly string[] ModuleAssemblyNames =
    [
        "Vitarish.Requests.Hosting",
        "Vitarish.Users.Hosting",
        "Vitarish.Parties.Hosting",
        "Vitarish.Platform.Interface",
        "Vitarish.Communications.Hosting",
        "Vitarish.Intelligence.Interface",
        "Vitarish.Knowledge.Interface",
        "Vitarish.Files.Interface",
        "Vitarish.Financial.Interface",
        "Vitarish.Search.Interface",
        "Vitarish.Contents.Interface",
        "Vitarish.Links.Interface",
        "Vitarish.Sea.Interface",
        "Vitarish.Appointments.Interface",
        "Vitarish.Commerce.Interface",
        "Vitarish.Social.Hosting",
        "Vitarish.Medical.Interface",
        "Vitarish.Feed.Infrastructure",
    ];

    public static List<IModule> GetModules() =>
        ModuleAssemblyNames
            .SelectMany(GetModuleTypes)
            .Select(moduleType => (IModule)Activator.CreateInstance(moduleType)!)
            .ToList();

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
            List<IModule> Modules = GetModules();

            builder.Services.TryAddSingleton<IReadOnlyList<IModule>>(Modules);

            foreach (IModule Module in Modules)
            {
                await Module.Register(builder, configureDbContext);
            }

            return builder;
        }
    }

    extension(WebApplication app)
    {
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
