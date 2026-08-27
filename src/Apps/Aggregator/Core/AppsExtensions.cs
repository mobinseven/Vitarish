using Vitarish.Clinic.App;
using Vitarish.Landing.App;
using Vitarish.ProjectManagement.App;
using Vitarish.Apps.Base.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Apps.Aggregator.Core;

public static class AppsExtensions
{
    public static List<IApp> GetApps(IConfiguration? configuration = null)
    {
        bool evaluated = configuration?.GetValue<bool>(Module.EvaluatedConfigurationEnabledKey) == true;
        string? profile = configuration?[Module.EvaluatedConfigurationIdentityKey];

        if (evaluated && !string.Equals(profile, Module.MainOnlyEvaluatedProfileIdentity, StringComparison.Ordinal) &&
            !string.Equals(profile, Module.ProductEvaluatedProfileIdentity, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unknown evaluated composition profile '{profile ?? ""}'.");

        if (evaluated && string.Equals(profile, Module.MainOnlyEvaluatedProfileIdentity, StringComparison.Ordinal))
            return [];

        return
        [
            Activator.CreateInstance<ClinicApp>(),
            Activator.CreateInstance<ProjectManagementApp>(),
            Activator.CreateInstance<LandingApp>(),
        ];
    }
    public static async Task<WebApplicationBuilder> RegisterApps(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        List<IApp> apps = GetApps(builder.Configuration);

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

    public static WebApplication MapAppEndpoints(this WebApplication app)
    {
        IReadOnlyList<IApp>? apps = app.Services.GetService<IReadOnlyList<IApp>>();
        if (apps is null)
        {
            return app;
        }

        foreach (IApp application in apps)
        {
            application.MapEndpoints(app);
        }

        return app;
    }
}
