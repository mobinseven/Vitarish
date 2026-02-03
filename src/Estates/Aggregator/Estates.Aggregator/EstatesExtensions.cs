using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Vitarish.Estates.Base;

public static class EstatesExtensions
{
    public static List<IEstate> DiscoverEstates()
    {
        var estates = new List<IEstate>();

        // Choose a root assembly (EntryAssembly is best for apps)
        Assembly rootAssembly =
            Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        // Load referenced assemblies + include the root itself
        var assemblies = rootAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Append(rootAssembly);

        foreach (var assembly in assemblies)
        {
            Type[] types;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Safely handle partially loadable assemblies
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            var estateTypes = types.Where(t =>
                typeof(IEstate).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract &&
                t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var estateType in estateTypes)
            {
                if (Activator.CreateInstance(estateType) is IEstate estate)
                {
                    estates.Add(estate);
                }
            }
        }

        return estates;
    }
    public static async Task<WebApplicationBuilder> RegisterEstates(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        List<IEstate> estates = DiscoverEstates();

        // Use TryAdd to avoid conflicts if estates are registered multiple times
        builder.Services.TryAddSingleton<IReadOnlyList<IEstate>>(estates);

        foreach (IEstate estate in estates)
        {
            await estate.Register(builder, configureDbContext);
        }

        return builder;
    }
    public static async Task<WebApplication> InitializeEstates(this WebApplication app)
    {
        IReadOnlyList<IEstate>? estates = app.Services.GetService<IReadOnlyList<IEstate>>();
        if (estates is null)
        {
            return app;
        }

        foreach (IEstate estate in estates)
        {
            await estate.Initialize(app);
        }

        return app;
    }
}
