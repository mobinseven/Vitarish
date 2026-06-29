using Vitarish.Clinic.App;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clinic.Interface;
using Vitarish.Modules.Base.Core.Endpoints;

public static class AppsExtensions
{
    public static List<IApp> GetApps() =>
            [
                Activator.CreateInstance<ClinicApp>(),
            ];
    public static async Task<WebApplicationBuilder> RegisterApps(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        List<IApp> apps = GetApps();

        builder.Services.TryAddSingleton<IReadOnlyList<IApp>>(apps);

        foreach (IApp app in apps)
        {
            await app.Register(builder, configureDbContext);
        }

        builder.Services.AddModuleOpenApiDocuments(apps.Select(a => a.Name));

        return builder;
    }
    public static async Task<WebApplication> InitializeApps(this WebApplication app)
    {
        IReadOnlyList<IApp>? apps = app.Services.GetService<IReadOnlyList<IApp>>();
        if (apps is null)
        {
            return app;
        }

        foreach (IApp ap in apps)
        {
            await ap.Initialize(app);
        }

        return app;
    }
}
