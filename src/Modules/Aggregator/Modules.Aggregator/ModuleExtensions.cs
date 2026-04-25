using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Communications.Hosting;
using Vitarish.Files.Interface;
using Vitarish.Instagram.Interface;
using Vitarish.Intelligence.Interface;
using Vitarish.Modules.Base;
using Vitarish.Search.Interface;
public static class ModulesExtensions
{
    public static List<IModule> GetModules() =>
            [
                Activator.CreateInstance<CommunicationModule>(),
                Activator.CreateInstance<IntelligenceModule>(),
                Activator.CreateInstance<FilesModule>(),
                Activator.CreateInstance<InstagramModule>(),
                Activator.CreateInstance<SearchModule>(),
            ];
    public static async Task<WebApplicationBuilder> RegisterModules(
        this WebApplicationBuilder builder,
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
    public static async Task<WebApplication> InitializeModules(this WebApplication app)
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
