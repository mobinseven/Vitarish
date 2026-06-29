using Finbuckle.MultiTenant;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Apps.Aggregator;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Audit.Server;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Feed.Infrastructure;
using Vitarish.Modules.Base.Interface.Services;
using Vitarish.Main.Authorization;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Handlers;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Infrastructure.Services;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Platform.Module.DependencyInjection;
using Vitarish.Main.Infrastructure;
using Vitarish.Modules.Base.Core;

if (args.Length >= 2 && args[0] == "--export-openapi")
{
    string outputDir = args[1];
    Directory.CreateDirectory(outputDir);
    await OpenApiExporter.ExportAsync(outputDir);
    return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

void configurePostgres(DbContextOptionsBuilder options)
{
    string? assemblyName = options.Options.ContextType.Assembly.FullName;
    if (assemblyName is null || !assemblyName.StartsWith("Vitarish", StringComparison.Ordinal))
    {
        assemblyName = "Vitarish.Main";
    }

    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Db"),
        b =>
        {
            _ = b.MigrationsAssembly(assemblyName);
            _ = b.EnableRetryOnFailure();
        });
}

_ = builder.Services.AddScoped<ITenantService, TenantService>();
_ = builder.Services.AddAuditInfrastructure(configurePostgres);

await builder.RegisterApps(configurePostgres);
await builder.RegisterModules(configurePostgres);

// Register Platform CircuitHandler for memory telemetry
builder.AddPlatformCircuitMemoryObserver();

// Register Feed infrastructure
_ = builder.Services.AddFeedInfrastructure();

builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddHubOptions(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    options.MaximumParallelInvocationsPerClient = 1;
                    options.MaximumReceiveMessageSize = 128 * 1024;
                });

_ = builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Pages";
});

builder.AddApplicationOptions();


await builder.AddHostIdentityServer(configurePostgres);

builder.AddHostAuthentication();
builder.AddHostAuthorization();
builder.AddHostHangfire(builder.Configuration.GetConnectionString("Db")!);
builder.AddHostPipeline();
builder.AddHostCors();

await builder.AddHostDataProtection();

_ = builder.Services.AddMemoryCache();
_ = builder.Services.AddHttpContextAccessor();
_ = builder.Services.AddDistributedMemoryCache();
_ = builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);  // Align with WebAuthn timeout (60s) + buffer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
_ = builder.Services.AddControllers();
_ = builder.Services.AddEndpointsApiExplorer();
// OpenAPI document generation is host-owned and exposed only in Development.
_ = builder.Services.AddOpenApi();
_ = builder.Services.AddLocalization();
Func<object, Task<string?>> tenantResolver = async context =>
{

    if (context is HttpContext http)
    {

        ITenantResolver tenantsService = http.RequestServices.GetRequiredService<ITenantResolver>();
        {
            Tenant? tenant = await tenantsService.ResolveAsync(http, http.RequestAborted);
            if (tenant is not null)
            {
                return tenant.Id.ToString();
            }
        }
    }
    else
    {
        throw new InvalidOperationException("context is not HttpContext");
    }

    return null;
}
;
_ = builder.Services.AddMultiTenant<TenantInfo>()
    .WithEchoStore()
    .WithDelegateStrategy(tenantResolver);

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();

builder.Services.AddSingleton(new NavServiceRoutes { ProfileRoute = AppRoutes.Account.Settings });

builder.Services.AddSingleton<IAppsProvider, AppsProvider>();

builder.Services.AddScoped<EntityDataProvider>();
builder.Services.AddScoped<IEntityUserService>(sp => sp.GetRequiredService<EntityDataProvider>());
builder.Services.AddScoped<IEntityTimeService>(sp => sp.GetRequiredService<EntityDataProvider>());

builder.Services.AddSingleton<INotificationHandler<EntityNotification>, EntityNotificationHandler>();

builder.Services.AddVitarishModulesBaseInterface();

WebApplication app = builder.Build();

_ = await app.MigrateDatabaseAsync<AuditDbContext>();

CookiePolicyOptions cookiePolicyOptions = new()
{
    Secure = CookieSecurePolicy.Always
};
RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("fa")
    .AddSupportedCultures(["en", "fa"])
    .AddSupportedUICultures(["en", "fa"]);

_ = app.UseForwardedHeaders();
_ = app.UseRequestLocalization(localizationOptions);
_ = app.UseCookiePolicy(cookiePolicyOptions);

_ = app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error");
_ = app.UseHsts();
_ = app.UseHttpsRedirection();
_ = app.MapStaticAssets();

_ = app.UseRouting();

_ = app.UseCors("WebHostCors");

_ = app.UseRateLimiter();

_ = app.UseSession();

_ = app.UseIdentityServer();

_ = app.UseAuthentication();
_ = app.UseAuthorization();

_ = app.UseAntiforgery();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi("/openapi/{documentName}.json");
}

_ = app.UseMultiTenant();

await app.InitializeModules();
await app.InitializeApps();

_ = app.MapRazorComponents<Vitarish.Apps.Base.Core.App>().AddInteractiveServerRenderMode();
_ = app.MapRazorPages();


app.MapHangfireDashboard();

app.ScheduleJobs();


app.Run();
